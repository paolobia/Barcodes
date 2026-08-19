# Barcodes Manager

🔗 **App online**: https://paolobia.github.io/Barcodes/

App web per gestire codici a barre: scansiona o genera un codice, associalo a un nome e a delle coordinate GPS, e ritrova rapidamente quello più vicino alla tua posizione attuale.

Realizzata come **Blazor WebAssembly (.NET 8)**, funziona interamente lato client — tutti i dati vengono salvati nel `localStorage` del browser: non c'è alcun backend né database. È installabile come **PWA**.

## Funzionalità

- Scansione di codici a barre dalla fotocamera (via [QuaggaJS](https://github.com/ericblade/quagga2))
- Generazione dell'immagine del codice a barre (via [JsBarcode](https://github.com/lindell/JsBarcode)), con riconoscimento automatico del formato (EAN8/UPC/EAN13/ITF14/CODE128) in base alla lunghezza del codice
- Geolocalizzazione: ogni codice può essere associato a latitudine/longitudine, e la lista viene ordinata per distanza dalla posizione corrente (formula di Haversine)
- Ricerca per nome
- Esportazione/importazione dei dati in formato JSON (backup manuale)
- Installabile come PWA (icona in home, funzionamento offline via service worker)

## Sviluppo

Requisiti: [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet restore
dotnet run                       # serve l'app sull'URL configurato in Properties/launchSettings.json
dotnet watch run                 # hot-reload durante lo sviluppo
```

Build di produzione:

```bash
dotnet publish -c Release --nologo
# output in bin/Release/net8.0/publish/wwwroot
```

Non esiste un progetto di test in questo repository.

## Note tecniche

- Geolocalizzazione e fotocamera richiedono un contesto sicuro (HTTPS o `localhost`) per funzionare nel browser.
- Il testo dell'interfaccia e i commenti nel codice sono in italiano.
- Struttura del progetto: `Pages/Home.razor` contiene la quasi totalità della logica applicativa; `Services/BarcodeService.cs` gestisce la persistenza su `localStorage`; `Services/GenerateBarcode.cs` genera l'immagine del barcode via interop JS; lo scanner (QuaggaJS) e le altre funzioni di interop sono definite direttamente in `wwwroot/index.html`.

## Deploy

Il deploy su [GitHub Pages](https://pages.github.com/) è automatizzato tramite GitHub Actions (`.github/workflows/deploy.yml`) a ogni push su `main`, pubblicato su https://paolobia.github.io/Barcodes/.
