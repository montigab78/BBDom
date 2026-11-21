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

## Differenze della copia prima della correzione dell'arresto

Confronto del 20 novembre 2026 tra `KNXLib` in BBDom (`65bddbb`) e `src/KNXLib` nel commit originale `6b50cef`, ignorando CRLF/LF e BOM: **27 dei 32 file comuni coincidono**. Le differenze allora presenti erano:

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

Il porting di BBDom a .NET 10 non ha incluso il retargeting di KNXLib. L'upstream ha introdotto l'intervallo di invio configurabile ([`1220360`](https://github.com/lifeemotions/knx.net/commit/1220360ee66f7e19d7fea8b5a4989f82e96c79f7)) e ha rivisto l'arresto dei thread ([`fa6536f`](https://github.com/lifeemotions/knx.net/commit/fa6536feacccda6d48b797759f4e156096e41d88)). La versione originale `1.1.10` usa `Thread.Abort` e `Thread.ResetAbort`, non supportati da .NET 10.

### 21 novembre 2026 — Correzione dell'arresto su .NET 10

L'esecuzione sulla macchina target ha confermato `PlatformNotSupportedException` durante lo stop. La copia locale ora usa arresto cooperativo, senza cambiare il target .NET Framework 4.7.2 o l'API pubblica:

- `KnxReceiver.Stop()` segnala l'arresto e completa la coda, sbloccando il consumer. La coda viene rilasciata dopo l'uscita dei due thread. I telegrammi ancora accodati non vengono consegnati durante lo stop, coerentemente con il precedente arresto forzato.
- I ricevitori chiudono i socket UDP per sbloccare le ricezioni pendenti. Il routing non mantiene più un thread inattivo in `Thread.Sleep`.
- Lo stop attende i thread con un limite di 5 secondi per thread; un eventuale timeout viene segnalato con un'eccezione. Se lo stop è richiesto da un callback del ricevitore o consumer, non attende i worker per evitare self-join e attese incrociate.
- Il tunneling invia la richiesta di disconnessione una sola volta per sessione e chiude comunque il ricevitore se l'invio fallisce. Una risposta del gateway durante lo stop non avvia una seconda disconnessione.
- La riconnessione chiude la vecchia sessione e crea una nuova coda e un nuovo ricevitore. Il timer non invia nuove richieste durante lo stop. Gli errori di rete effettivi restano registrati; gli errori di socket previsti durante la chiusura sono distinti da quelli della normale ricezione.

Verifica sul runtime .NET 10 con un harness temporaneo e socket solo locali, ripetuta cinque volte: 100 cicli tunneling con eventi, risposte di stato, stop a ricezione inattiva, riutilizzo della porta e riconnessione; 50 race fra stop e risposta del gateway; stop richiesto dal ricevitore e dal consumer; stop sotto traffico di 200 telegrammi; chiusura garantita anche con invio di disconnessione fallito; 50 cicli di ricezione/stop routing simulato. Controllati entrambi i thread terminati, stop normale sotto 2 secondi, singola notifica di disconnessione per sessione e assenza di errori nei percorsi normali. Le prove routing usano socket unicast locali, non un impianto multicast reale.

Resta da pubblicare questa versione sulla macchina target e verificare avvio, stop e successivo riavvio con il gateway reale. Le conversioni locali e il protocollo dei telegrammi non sono stati modificati.

### Diagnostica di ricezione e ciclo di vita

I log distinguono tentativo di connessione, sostituzione della vecchia sessione, risposta del gateway con canale e stato, tunnel stabilito, perdita della connessione e disconnessione richiesta. La diagnostica `Debug` riporta dimensione e mittente dei datagrammi, servizio KNXnet/IP, scarti per canale o sequenza, invio degli ACK, completamento della coda, chiusura UDP e uscita dei worker. I timeout di arresto sono registrati come errori. Gli eventi e le risposte di stato mantengono i log già presenti in `KnxConnection`; i dettagli cEMI aggiuntivi dipendono da `connection.Debug`.

Il consumer Web attuale inoltra tutti gli eventi della libreria con `LogInformation`, anche quelli originariamente `Debug`, `Warn` o `Error`: nel file NLog possono quindi apparire come `INFO`. La correzione non cambia questo instradamento. I log di ricezione sono frequenti e contengono endpoint e indirizzi di gruppo: non distribuirli senza verificare i dati presenti.

Per evitare gli avvisi di Visual Studio sui fine riga, i cinque sorgenti KNXLib coinvolti nella correzione sono mantenuti uniformemente in CRLF, preservando la codifica esistente. Le modifiche successive devono mantenere questo formato.

## Uso commerciale e alternative

Ricognizione del 20 novembre 2026; nessuna dipendenza sostituita. La licenza MIT di KNX.net permette uso commerciale, modifica e distribuzione conservando gli avvisi di copyright e licenza. Manutenzione e compatibilità a runtime restano a carico di chi distribuisce la copia.

| Alternativa | Licenza e note |
| --- | --- |
| [KNX Falcon SDK 6.4.8671](https://www.nuget.org/packages/Knx.Falcon.Sdk/6.4.8671) | SDK ufficiale proprietario; target .NET 8 utilizzabile da .NET 10. Verificare la [licenza KNX Tools](https://support.knx.org/hc/en-us/articles/360002909959) per il proprio modello di distribuzione. |
| [knx-dotnet](https://www.nuget.org/packages/knx-dotnet/) | MIT; documenta tunneling, routing e discovery, ma disponibile solo in prerelease su NuGet alla data della ricognizione. |
| [Tiveria.Home.Knx.IP 2.0.81](https://www.nuget.org/packages/Tiveria.Home.Knx.IP/2.0.81) | LGPL-3.0-only; documenta tunneling, routing e codec DPT. Uso commerciale possibile nel rispetto degli obblighi LGPL di distribuzione, inclusa la possibilità di sostituire o ricollegare la libreria. |

Per una sostituzione commerciale, Falcon è il primo candidato da provare se i suoi termini coprono la distribuzione. Se la priorità è conservare una licenza permissiva, valutare prima il porting della copia MIT su .NET 10.
