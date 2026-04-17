# Build context: frontend/
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
RUN npm run build:prod

# Sortie Angular : dist/smartbank-frontend/ ou dist/smartbank-frontend/browser/
RUN mkdir -p /tmp/web && \
    if [ -f dist/smartbank-frontend/browser/index.html ]; then \
      cp -r dist/smartbank-frontend/browser/. /tmp/web/; \
    else \
      cp -r dist/smartbank-frontend/. /tmp/web/; \
    fi

FROM nginx:alpine
COPY nginx.docker.conf /etc/nginx/conf.d/default.conf
COPY --from=build /tmp/web /usr/share/nginx/html
EXPOSE 80
