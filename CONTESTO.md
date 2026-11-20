# Contesto del progetto e delle chat

## Progetto

BBDom gestisce la connessione verso un gateway KNX TCP/IP e il controllo domotico di luci, tapparelle e termostati.

## Preferenze di lavoro

- Aggiornare questo file con il contesto, le decisioni e gli esiti rilevanti delle chat, senza includere credenziali o dati dell'impianto.
- Preferire `if`/`else` agli operatori ternari.
- Tenere gli argomenti sulla stessa riga invece di andare a capo per ciascun argomento.
- Mantenere le graffe anche per i blocchi di una sola istruzione.

## 20 novembre 2026 — Aggiornamento a .NET 10

Il primo intervento richiesto è migrare BBDom a .NET 10 mantenendo lo stile esistente e preservando il comportamento domotico.

- `BBDom.Web` e `BBDom.Data` partivano da .NET 5; `BBDom.Biz.DotNet` da .NET Framework 4.7.2. Questi tre progetti usano ora `net10.0` e il formato SDK.
- `BBDom.Web` ospita l'applicazione ASP.NET Core; `BBDom.Data` gestisce la persistenza SQLite e le migrazioni EF Core. Il Generic Host di .NET sostituisce Topshelf per il servizio Windows `BBDom.Web`; l'avvio in console resta disponibile.
- `dotnet ef migrations has-pending-model-changes` non ha rilevato differenze rispetto all'ultima migrazione; il database non è stato modificato.

### Precisazione: KNXLib fuori dal perimetro della migrazione

`KNXLib` è una libreria esterna inclusa nella soluzione che implementa tunneling e routing KNX/IP su UDP. Su richiesta dell'utente, sono state annullate le modifiche della prima migrazione alla libreria: sorgenti e progetto .NET Framework 4.7.2 rimangono invariati. Ogni intervento su `KNXLib` sarà oggetto di una richiesta separata. Il precedente test di riconnessione in loopback riguardava la versione modificata e non vale come verifica della versione ripristinata.

Con il riferimento originale sono state verificate le build Debug e Release da riga di comando e la pubblicazione, che include la DLL .NET Framework 4.7.2. Se Visual Studio ha ancora caricato la versione SDK del progetto, chiudere e riaprire la soluzione prima di ricompilare.

La libreria contiene ancora `Thread.Abort` e `Thread.ResetAbort`, API non supportate dal runtime .NET 10: la build non garantisce il corretto funzionamento di tutti i percorsi di arresto e riconnessione. Il gateway reale e l'avvio come servizio Windows restano da verificare.

### Aggiornamento del logging

`BBDom.Web` usa `NLog.Web.AspNetCore` 6.3.0 con NLog 6.2.2. La configurazione è nella sezione `NLog` di `appsettings.json` anche per il logger di avvio. Rimangono i target console e file, il livello `Trace`, la rotazione giornaliera e il limite di 10 archivi. `KNXLib` rimane invariata.
