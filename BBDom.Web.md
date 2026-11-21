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

## Installazione come servizio Windows

I comandi seguenti sono un esempio per una **nuova installazione** in `C:\Services\BBDom.Web`. Eseguirli in PowerShell come amministratore. Il servizio si chiama `BBDom.Web` e deve essere avviato senza `--console`; i comandi Topshelf `install` e `uninstall` non si applicano più.

### 1. Pubblicazione

Sulla macchina di compilazione Windows servono un SDK compatibile con .NET 10 e il targeting pack .NET Framework 4.7.2 per KNXLib. Dalla radice della soluzione pubblicare in una cartella separata:

```powershell
dotnet publish .\BBDom.Web\BBDom.Web.csproj -c Release --output .\BBDom.Web\bin\Release\service-publish
if ($LASTEXITCODE -ne 0) { throw 'Pubblicazione non riuscita' }
```

La pubblicazione è framework-dependent: sulla macchina che esegue il servizio deve essere installato il **runtime ASP.NET Core 10** per l'architettura appropriata. Verificare con `dotnet --list-runtimes`. Copiare l'intero contenuto di `service-publish` in una cartella dedicata sulla macchina di destinazione, ad esempio `C:\Services\BBDom.Web`, mantenendo `appsettings.json` accanto a `BBDom.Web.exe`.

Prima di copiare su un'installazione esistente, arrestare il servizio e salvare database e configurazione. Non sovrascrivere un `appsettings.json` personalizzato o il database con i file della pubblicazione senza averli confrontati. Le istruzioni sotto creano un servizio nuovo: se `BBDom.Web` esiste già, esaminare la sua configurazione con `sc.exe qc BBDom.Web` prima di intervenire.

### 2. Configurazione e permessi

Prima dell'avvio controllare gli endpoint Kestrel e `ConnectionStrings:BBDomConnection` nel file pubblicato, oltre agli indirizzi del gateway nel codice del manager. Usare per SQLite un **percorso assoluto** verso il database previsto: la connection string predefinita usa `DataSource=bb_dom.db`, un percorso relativo risolto rispetto alla directory corrente del processo, che per un servizio Windows può essere diversa dalla cartella dell'eseguibile. Impostare per esempio la proprietà JSON `BBDomConnection` su `DataSource=C:\\Dati\\BBDom\\bb_dom.db`, adattando il percorso; nel JSON i separatori `\` vanno raddoppiati.

`New-Service` senza credenziali esegue il servizio come **LocalSystem**. Assicurarsi che l'account scelto abbia accesso al database e alla sua cartella, anche in scrittura, alla cartella di log configurata e alla rete KNX. Controllare inoltre firewall e porta HTTP configurata. Non copiare né distribuire credenziali nel repository.

### 3. Registrazione e avvio

Verificare che il servizio non esista già e che l'eseguibile sia presente, quindi registrarlo con avvio automatico:

```powershell
if (Get-Service -Name 'BBDom.Web' -ErrorAction SilentlyContinue) { throw 'Il servizio BBDom.Web esiste già: verificarne la configurazione prima di procedere.' }
if (!(Test-Path -LiteralPath 'C:\Program Files\BBDom.Web\BBDom.Web.exe' -PathType Leaf)) { throw 'Eseguibile pubblicato non trovato.' }
New-Service -Name 'BBDom.Web' -BinaryPathName '"C:\Program Files\BBDom.Web\BBDom.Web.exe"' -DisplayName 'BBDom.Web' -Description 'BBDom.Web Service' -StartupType Automatic
```

Avviare **solo dopo** aver verificato configurazione e impianto: l'avvio può inviare telegrammi KNX e comandi di automazione.

```powershell
Start-Service -Name 'BBDom.Web'
Get-Service -Name 'BBDom.Web'
sc.exe qc BBDom.Web
```

Controllare anche `C:\Log\BBDom.Web\debug.log` o il percorso NLog configurato e, in caso di errore di avvio, il Visualizzatore eventi di Windows. Lo stato `Running` conferma che il servizio è avviato, non che il gateway o le automazioni funzionino correttamente.

### 4. Arresto, aggiornamento e rimozione

```powershell
Stop-Service -Name 'BBDom.Web'
Get-Service -Name 'BBDom.Web'
```

Per aggiornare un'installazione esistente, attendere che il servizio sia fermo, salvare database e configurazione, sostituire i file pubblicati preservando i dati personalizzati e riavviare con `Start-Service -Name 'BBDom.Web'`. Se l'eseguibile cambia percorso, aggiornare la configurazione del servizio dopo aver verificato il nuovo percorso.

Per rimuovere la registrazione, dopo l'arresto:

```powershell
sc.exe delete BBDom.Web
```

La rimozione del servizio non cancella i file pubblicati, il database o i log. Non rimuoverli senza verificarne l'utilizzo e averne eseguito il backup.

## Verifiche e limiti

Durante il porting sono state verificate le build Debug e Release, la pubblicazione su cartella e l'assenza di modifiche al modello rispetto all'ultima migrazione EF Core. Il database non è stato modificato. La migrazione di NLog è stata verificata con una pubblicazione senza XML e una prova locale di scrittura su console e file, inclusa la rotazione.

L'avvio dell'app attiva il manager e può inviare comandi all'impianto: non usarlo come smoke test contro il gateway reale senza averlo previsto. Le istruzioni di installazione sopra non sono state eseguite su un servizio reale; restano da verificare sul sistema di destinazione l'avvio dal Service Control Manager e i percorsi di arresto e riconnessione. Il precedente test in loopback riguardava una modifica poi annullata a KNXLib, non la versione integrata ora.
