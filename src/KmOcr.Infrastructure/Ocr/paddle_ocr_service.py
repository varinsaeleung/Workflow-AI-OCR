import argparse
import json
import os
import statistics
import sys
import tempfile
from pathlib import Path


def parse_arguments():
    """Parse command-line arguments for the PaddleOCR adapter."""
    parser = argparse.ArgumentParser(description="Run PaddleOCR and emit normalized JSON.")
    parser.add_argument("--input", required=True, help="Input PDF, JPG, or PNG path.")
    parser.add_argument("--content-type", required=True, help="Input MIME content type.")
    parser.add_argument("--languages", default="tha+eng", help="Requested OCR languages.")
    return parser.parse_args()


def build_reader(languages):
    """Create a PaddleOCR reader configured for Thai and English recognition."""
    from paddleocr import PaddleOCR

    lang = "th" if "tha" in languages.lower() else "en"
    return PaddleOCR(use_angle_cls=True, lang=lang, show_log=False)


def convert_pdf_to_images(input_path):
    """Convert PDF pages to temporary images for PaddleOCR processing."""
    from pdf2image import convert_from_path

    temp_dir = tempfile.mkdtemp(prefix="km-ocr-pdf-")
    images = convert_from_path(input_path, dpi=200, output_folder=temp_dir, fmt="png")
    paths = []
    for index, image in enumerate(images, start=1):
        page_path = os.path.join(temp_dir, f"page-{index}.png")
        image.save(page_path, "PNG")
        paths.append(page_path)
    return paths


def get_page_inputs(input_path, content_type):
    """Return image paths that PaddleOCR can process."""
    if content_type.lower() == "application/pdf":
        return convert_pdf_to_images(input_path)
    return [input_path]


def normalize_box(box):
    """Convert a PaddleOCR polygon into x, y, width, and height values."""
    xs = [point[0] for point in box]
    ys = [point[1] for point in box]
    return {
        "x": round(min(xs), 2),
        "y": round(min(ys), 2),
        "width": round(max(xs) - min(xs), 2),
        "height": round(max(ys) - min(ys), 2),
    }


def read_page(reader, image_path, page_number):
    """Run OCR for one image page and return normalized page JSON."""
    raw_result = reader.ocr(image_path, cls=True)
    lines = raw_result[0] if raw_result and raw_result[0] else []
    words = []

    for item in lines:
        box = item[0]
        text = item[1][0]
        score = float(item[1][1])
        bounds = normalize_box(box)
        words.append({
            "text": text,
            "confidenceScore": round(score, 4),
            **bounds,
        })

    page_text = "\n".join(word["text"] for word in words)
    scores = [word["confidenceScore"] for word in words]
    page_confidence = statistics.mean(scores) if scores else 0
    return {
        "pageNumber": page_number,
        "text": page_text,
        "confidenceScore": round(page_confidence, 4),
        "words": words,
    }


def main():
    """Run PaddleOCR and write normalized JSON to stdout."""
    args = parse_arguments()
    input_path = Path(args.input)
    if not input_path.exists():
        raise FileNotFoundError(f"Input file does not exist: {input_path}")

    reader = build_reader(args.languages)
    page_inputs = get_page_inputs(str(input_path), args.content_type)
    pages = [read_page(reader, page_path, index) for index, page_path in enumerate(page_inputs, start=1)]
    payload = {
        "engine": "paddleocr",
        "language": args.languages,
        "pages": pages,
    }
    print(json.dumps(payload, ensure_ascii=False))


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(str(exc), file=sys.stderr)
        sys.exit(1)
