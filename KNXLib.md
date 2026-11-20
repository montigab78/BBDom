# KNXLib — provenienza, architettura e integrazione

`KNXLib` è una copia del sorgente KNX.net inclusa nella soluzione BBDom, non un submodule né un riferimento a un pacchetto NuGet. È utilizzata da `BBDom.Web` e `BBDom.Biz.DotNet`; per il flusso del servizio Web si veda [BBDom.Web.md](BBDom.Web.md).

## Repository di origine

- **Originale:** [lifeemotions/knx.net](https://github.com/lifeemotions/knx.net). Alla verifica del 20 novembre 2026 il branch `master` punta a [`6b50cef`](https://github.com/lifeemotions/knx.net/commit/6b50cefa6fe24794efe1a86b089139babcb8c509), del 30 giugno 2018. È il commit che allinea i metadati alla versione `1.1.10`, successivo al [tag `1.1.10`](https://github.com/lifeemotions/knx.net/tree/1.1.10). Il repository non risulta formalmente archiviato, ma non ci sono commit più recenti sul branch predefinito.
- **Fork di partenza:** [montigab78/knx.net](https://github.com/montigab78/knx.net), il cui `master` punta a [`e5d9b9d`](https://github.com/montigab78/knx.net/commit/e5d9b9db6db987f7fb066e0211251a122a529bfb), del 16 maggio 2017, relativo al DPT `5.001`. È la base del confronto storico, non un commit d'importazione dichiarato nei metadati di BBDom.
- **Perimetro importato:** [`src/KNXLib`](https://github.com/lifeemotions/knx.net/tree/6b50cefa6fe24794efe1a86b089139babcb8c509/src/KNXLib) e la [licenza MIT locale](KNXLib/LICENSE). Il repository originale contiene anche [`tests`](https://github.com/lifeemotions/knx.net/tree/6b50cefa6fe24794efe1a86b089139babcb8c509/tests), con `KNXLibTests` e i progetti dimostrativi `KNXTestRouting` e `KNXTestTunneling`, non importati in BBDom. Gli esempi di utilizzo sono nel [README upstream](https://github.com/lifeemotions/knx.net/blob/6b50cefa6fe24794efe1a86b089139babcb8c509/README.md).

## Architettura e contratto con BBDom

| Area | Implementazione |
| --- | --- |
| API pubblica | `KnxConnection` espone `Connect`, `Disconnect`, `Action`, `RequestStatus`, `ToDataPoint` e `FromDataPoint`; i delegate distinguono connessione, disconnessione, eventi spontanei e risposte alle letture. |
| Trasporto | `KnxConnectionTunneling` gestisce un canale KNXnet/IP UDP verso il gateway, con timer di controllo della connessione e componenti dedicati a ricezione e invio. `KnxConnectionRouting` usa UDP multicast; BBDom usa il tunneling. |
| Ricezione | `KnxReceiver` decodifica i frame cEMI; accoda gli eventi a un thread consumer e inoltra separatamente le risposte di stato. |
| Invio | `KnxLockManager` serializza le operazioni con un semaforo. `SetLockIntervalMs` imposta l'intervallo tra gli invii, predefinito a 200 ms. |
| Conversioni | `DPT/DataPointTranslator.cs` registra i codec per ID tramite reflection. L'originale documenta `3.007`, `3.008`, `5.001`, `5.003`, `5.004`, `5.010`, `6.001`, `6.010` e `9.001`; BBDom aggiunge i tipi indicati sotto. |
| Logging | La libreria espone eventi statici tramite `KNXLib.Log.Logger`. I consumer li inoltrano ai propri logger; NLog non è una dipendenza di KNXLib. |

## Differenze della copia attuale

Confronto tra `KNXLib` in BBDom (`65bddbb`) e `src/KNXLib` nel commit originale `6b50cef`, ignorando CRLF/LF e BOM: **27 dei 32 file comuni coincidono**. Le differenze residue sono:

| File | Differenza rispetto all'originale |
| --- | --- |
| `DPT/DataPointBit.cs` | File solo locale. Gestisce come booleani `1.001`, `1.002`, `1.008`, `1.009` e `1.100`. |
| `DPT/DataPoint8BitSignRelativeValue.cs` | `20.102` è associato allo stesso codec di `6.001` e `6.010`, senza cambiare la conversione. È una scelta della copia BBDom, non un'implementazione upstream specifica per `20.102`. |
| `KnxLockManager.cs` | Se la connessione non è attiva, `PerformLockedOperation` ritorna prima di attendere il semaforo. La guardia non copre tutte le condizioni di concorrenza. |
| `KnxReceiverTunneling.cs` | Aggiunge diagnostica `Debug` sui datagrammi ricevuti. |
| `Log/Logger.cs` | Differenza residua di formattazione; nome pubblico `Logger` e contratto degli eventi allineati all'upstream. |
| `KNXLib.csproj` | Progetto classico con .NET Framework 4.7.2 (upstream: 4.0), `ToolsVersion` 12.0 e inclusione di `DataPointBit.cs`. L'assembly è alla versione `1.1.10`. |
| `LICENSE` | File presente nella cartella locale e alla radice dell'upstream, non sotto `src/KNXLib`. |

### Cronologia locale

- `96d0b2c` (7 gennaio 2021): prima importazione in Git, già con quattro ID booleani. `d669969` (9 gennaio 2021) aggiunge `1.100`. `c2a9809` (9 gennaio 2024) aggiunge `Log/Logger.cs` a Git; i riferimenti al precedente nome `_logger` erano già presenti nel 2021.
- Prima dell'aggiornamento, il confronto di `28d9170` con il fork `e5d9b9d` dava 17 file uguali, 15 diversi e `DataPointBit.cs` solo locale, ignorando i fine riga. Molte differenze erano dovute alla rinomina del logger.
- L'importazione dell'upstream `1.1.10` e la riapplicazione delle modifiche locali sono state inizialmente separate fra staging e working tree per la revisione. Lo stato attuale è registrato nei commit `91a80cb` e `4ddcad7`; `65bddbb` allinea i due consumer al nome upstream `Logger`.

## Compatibilità e prossime verifiche

Il porting di BBDom a .NET 10 non ha incluso il retargeting di KNXLib. L'upstream ha introdotto l'intervallo di invio configurabile ([`1220360`](https://github.com/lifeemotions/knx.net/commit/1220360ee66f7e19d7fea8b5a4989f82e96c79f7)) e ha rivisto l'arresto dei thread ([`fa6536f`](https://github.com/lifeemotions/knx.net/commit/fa6536feacccda6d48b797759f4e156096e41d88)). Anche `1.1.10` usa ancora `Thread.Abort` e `Thread.ResetAbort`, non supportati da .NET 10. Una build riuscita non garantisce quindi l'arresto o la riconnessione. Il precedente test loopback riguardava una variante poi annullata.

Prima di sostituire o portare la libreria: confermare il trasporto effettivamente richiesto dal gateway, preservare la distinzione tra letture, scritture e telegrammi spontanei, verificare arresto e ripetuti cicli di riconnessione, e riprodurre le conversioni locali (in particolare `1.100` e `20.102`). Questa analisi è statica e non ha contattato il gateway.

## Uso commerciale e alternative

Ricognizione del 20 novembre 2026; nessuna dipendenza sostituita. La licenza MIT di KNX.net permette uso commerciale, modifica e distribuzione conservando gli avvisi di copyright e licenza. Manutenzione e compatibilità a runtime restano a carico di chi distribuisce la copia.

| Alternativa | Licenza e note |
| --- | --- |
| [KNX Falcon SDK 6.4.8671](https://www.nuget.org/packages/Knx.Falcon.Sdk/6.4.8671) | SDK ufficiale proprietario; target .NET 8 utilizzabile da .NET 10. Verificare la [licenza KNX Tools](https://support.knx.org/hc/en-us/articles/360002909959) per il proprio modello di distribuzione. |
| [knx-dotnet](https://www.nuget.org/packages/knx-dotnet/) | MIT; documenta tunneling, routing e discovery, ma disponibile solo in prerelease su NuGet alla data della ricognizione. |
| [Tiveria.Home.Knx.IP 2.0.81](https://www.nuget.org/packages/Tiveria.Home.Knx.IP/2.0.81) | LGPL-3.0-only; documenta tunneling, routing e codec DPT. Uso commerciale possibile nel rispetto degli obblighi LGPL di distribuzione, inclusa la possibilità di sostituire o ricollegare la libreria. |

Per una sostituzione commerciale, Falcon è il primo candidato da provare se i suoi termini coprono la distribuzione. Se la priorità è conservare una licenza permissiva, valutare prima il porting della copia MIT su .NET 10.
