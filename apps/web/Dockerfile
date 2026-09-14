FROM node:24-alpine AS build
WORKDIR /app

COPY apps/web/package*.json ./
RUN npm ci

COPY apps/web .
RUN npm run build

FROM nginx:1.29-alpine AS runtime
COPY infrastructure/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
RUN chown -R nginx:nginx /var/cache/nginx /var/run /etc/nginx/conf.d /usr/share/nginx/html
EXPOSE 8080
USER nginx
