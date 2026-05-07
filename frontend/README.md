# Fleet Management Frontend

Vue 3 + Vite + Vuetify frontend for Fleet Management.

## Run

```bash
npm install
npm run dev
```

The dev URL is printed by Vite, usually:

```text
http://localhost:5173
```

The frontend expects the API at:

```text
http://localhost:5215
```

## Build

```bash
npm run build
```

## Architecture Notes

- Pages live in `src/pages`
- Larger feature components live in `src/components`
- API clients live in `src/services`
- Shared utilities live in `src/utils`
- Shared composables live in `src/composables`
- Vue files use external CSS files through `<style src="...">`

## Auth

Login stores the JWT-backed session locally. API calls include the bearer token and role/user headers through the shared HTTP client.

## Reports

Reports support PDF and Excel-compatible exports from the frontend using data loaded from backend report APIs.
