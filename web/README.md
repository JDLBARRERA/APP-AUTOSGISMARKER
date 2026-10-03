# Consola AUTOSGISMARKER

Teléfono de árbitro y vista del proyector. El mapa del proyecto completo está en el [README de la raíz](../README.md).

```bash
npm install
npm run dev
```

- Consola: [http://localhost:3000](http://localhost:3000)
- Proyector: [http://localhost:3000/proyector](http://localhost:3000/proyector)
- Estadísticas: [http://localhost:3000/stats](http://localhost:3000/stats)

La consola y el proyector se hablan por `GET` y `POST` en `/api/session`. El estado vive en el proceso de `next dev`.
