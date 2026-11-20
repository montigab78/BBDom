# BBDom.Web — architettura e contesto operativo

BBDom è un'applicazione domotica per luci, tapparelle e termostati. Il servizio Web si collega al gateway con **KNXnet/IP tunneling su UDP**, non con un socket TCP. Provenienza, personalizzazioni e limiti della libreria sono documentati in [KNXLib.md](KNXLib.md). Il [README](README.md) contiene i comandi operativi.

## Componenti e flusso

| Componente | Responsabilità |
| --- | --- |
| `BBDom.Web/Program.cs` | Configura il Generic Host, il logging NLog e l'integrazione con il Service Control Manager. `--console` disattiva l'integrazione come servizio Windows. In modalità servizio, la radice dei contenuti è la cartella dell'eseguibile. |
| `BBDom.Web/Startup.cs` | Registra SQLite, ASP.NET Core Identity, MVC e il singleton `KnxConnectionManager`. Collega `Init()` e `Shutdown()` rispettivamente all'avvio e all'arresto dell'host. |
| `BBDom.Web/KnxConnectionManager.cs` | Inizializza i gruppi, apre il tunnel, gestisce i tentativi di riconnessione e distingue risposte alle letture da telegrammi spontanei. Contiene le automazioni domestiche e il calendario giornaliero degli offset termici (Ical.Net). Inoltra a `ILogger` i messaggi di `KNXLib.Log.Logger`. |
| `BBDom.Web/Controllers/HomeController.cs` | Espone le viste Home, Privacy ed Error; non è il punto di ingresso delle automazioni KNX. |
| `BBDom.Data` | Fornisce `BBDomDbContext`, che integra Identity e la persistenza SQLite di gruppi (`KnxGroups`) e stati (`KnxStates`). Gli stati distinguono risposte (`STATUS`) ed eventi (`EVENT`). |

Il server Web non usa `BBDom.Biz.DotNet`: entrambi sono consumer separati di `KNXLib`. Gli endpoint del gateway, gli indirizzi di gruppo delle automazioni e le fasce termiche sono definiti nel codice del manager; non sono documentati qui come configurazione replicabile dell'impianto.

## Configurazione e rilascio

- `BBDom.Web`, `BBDom.Data` e `BBDom.Biz.DotNet` usano `net10.0` e il formato SDK. I primi due provenivano da .NET 5, il terzo da .NET Framework 4.7.2. `KNXLib` mantiene .NET Framework 4.7.2.
- Il Generic Host ha sostituito Topshelf. Il servizio conserva il nome `BBDom.Web`; i comandi `install` e `uninstall` di Topshelf non sono più disponibili. Per l'installazione e l'aggiornamento usare il Service Control Manager.
- `BBDom.Web/appsettings.json` contiene gli endpoint Kestrel, `ConnectionStrings:BBDomConnection` e la sezione `NLog`. Il logger di bootstrap usa anch'esso `appsettings.json`; non c'è più `nlog.config`. `NLog.Web.AspNetCore` 6.3.0 risolve NLog 6.2.2. Il logging usa console e file `C:\Log\BBDom.Web\debug.log`, con livello minimo `Trace`, rotazione giornaliera e fino a 10 archivi.
- `FolderProfile` pubblica in modalità framework-dependent per il runtime ASP.NET Core 10. Lo strumento locale `dotnet-ef` si ripristina dalla cartella `BBDom.Web`; le migrazioni risiedono in `BBDom.Data`.

## Verifiche e limiti

Durante il porting sono state verificate le build Debug e Release, la pubblicazione su cartella e l'assenza di modifiche al modello rispetto all'ultima migrazione EF Core. Il database non è stato modificato. La migrazione di NLog è stata verificata con una pubblicazione senza XML e una prova locale di scrittura su console e file, inclusa la rotazione.

L'avvio dell'app attiva il manager e può inviare comandi all'impianto: non usarlo come smoke test contro il gateway reale senza averlo previsto. Restano da verificare sul sistema di destinazione l'avvio dal Service Control Manager e i percorsi di arresto e riconnessione. Il precedente test in loopback riguardava una modifica poi annullata a KNXLib, non la versione integrata ora.
