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

### Provenienza e differenze di KNXLib

La libreria è stata copiata nella soluzione, non inclusa come submodule. Il repository indicato, [montigab78/knx.net](https://github.com/montigab78/knx.net), è un fork di [lifeemotions/knx.net](https://github.com/lifeemotions/knx.net). Alla verifica del 20 novembre 2026, `master` del fork era fermo al [commit `e5d9b9d`](https://github.com/montigab78/knx.net/commit/e5d9b9db6db987f7fb066e0211251a122a529bfb) del 16 maggio 2017, relativo al DPT `5.001`. È il riferimento usato per il confronto, non un commit di importazione dichiarato nei metadati locali.

Confronto tra `KNXLib` in BBDom (`28d9170`) e `src/KNXLib` nel fork, ignorando le differenze di fine riga CRLF/LF: su 32 file comuni, 17 coincidono e 15 hanno differenze; un file è solo in BBDom e nessun file della cartella del fork manca nella copia locale. Il confronto non include i test, gli esempi o il sito del repository esterno.

| Area | Differenza locale rispetto al fork |
| --- | --- |
| DPT a un bit | `DPT/DataPointBit.cs` esiste solo in BBDom. Gestisce `1.001`, `1.002`, `1.008`, `1.009` e `1.100` come valori booleani. |
| DPT relativo | `DPT/DataPoint8BitSignRelativeValue.cs` aggiunge `20.102` agli ID già gestiti (`6.001`, `6.010`), senza cambiare la conversione. |
| Invio | `KnxLockManager.PerformLockedOperation` ritorna subito se la connessione non è attiva, senza attendere il semaforo né eseguire l'operazione. È una guardia contro l'attesa quando si è disconnessi, non una garanzia contro tutte le condizioni di concorrenza. |
| Logging | La classe pubblica `KNXLib.Log.Logger` è rinominata in `KNXLib.Log._logger` e i riferimenti interni sono aggiornati. `BBDom.Web/KnxConnectionManager.cs` e `BBDom.Biz.DotNet/Knx.cs` si iscrivono ai quattro eventi di log tramite questo nome. Nel ricevitore tunneling ci sono anche diagnostiche commentate senza effetto a runtime. |
| Progetto | `KNXLib.csproj` passa da .NET Framework 4.0 a 4.7.2, da MSBuild `ToolsVersion` 4.0 a 12.0 e include `DataPointBit.cs`; la versione assembly locale resta `1.1.9`. |

La cronologia di BBDom distingue l'importazione iniziale (`96d0b2c`, 7 gennaio 2021), che contiene già `DataPointBit` con i primi quattro ID, il commit `d669969` (9 gennaio 2021), che aggiunge `1.100`, e `c2a9809` (9 gennaio 2024), che aggiunge a Git `Log/Logger.cs`. Quest'ultima data non prova quando sia avvenuta la rinomina: i riferimenti a `_logger` sono già presenti nei file importati nel 2021.

Il `master` del progetto originale arriva al [commit `6b50cef`](https://github.com/lifeemotions/knx.net/commit/6b50cefa6fe24794efe1a86b089139babcb8c509) del 30 giugno 2018 (`1.1.10`), successivo al fork. Include due modifiche assenti nella copia BBDom analizzata: intervallo configurabile fra gli invii ([`1220360`](https://github.com/lifeemotions/knx.net/commit/1220360ee66f7e19d7fea8b5a4989f82e96c79f7)) e un intervento sui thread lasciati attivi ([`fa6536f`](https://github.com/lifeemotions/knx.net/commit/fa6536feacccda6d48b797759f4e156096e41d88)). Quest'ultimo usa ancora `Thread.Abort` e `Thread.ResetAbort` e non risolve la compatibilità con .NET 10.

### Importazione di KNXLib 1.1.10 e riapplicazione delle modifiche BBDom

Su richiesta dell'utente, è stato importato `src/KNXLib` da `master` del progetto originale a `6b50cef` (successivo al tag `1.1.10`), insieme alla sua licenza MIT. Lo staging contiene solo la copia upstream; le personalizzazioni sono state riapplicate sopra di essa con un confronto a tre vie basato su `e5d9b9d`, senza includerle nello staging. Non è stato creato alcun commit.

- `git diff --cached -- KNXLib` mostra l'importazione upstream senza le modifiche locali.
- `git diff -- KNXLib` mostra le personalizzazioni riapplicate, incluso `DataPointBit.cs` tramite intent-to-add. La libreria nel working tree resta su .NET Framework 4.7.2, con versione assembly `1.1.10`.

Non è stato avviato il gateway. La compatibilità di arresto e riconnessione su .NET 10 resta un intervento separato.
