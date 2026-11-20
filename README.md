# BBDom

Applicazione domotica per luci, tapparelle e termostati tramite gateway KNX/IP. Il contesto di lavoro e le decisioni emerse dalle chat sono in [CONTESTO.md](CONTESTO.md).

## Compilazione e pubblicazione

I progetti BBDom usano .NET 10. La libreria esterna `KNXLib` mantiene il progetto originale .NET Framework 4.7.2. Per compilare la soluzione su Windows servono un SDK compatibile con `net10.0` e il targeting pack .NET Framework 4.7.2.

```powershell
dotnet build .\BBDom.sln
dotnet publish .\BBDom.Web\BBDom.Web.csproj -c Release -p:PublishProfile=FolderProfile
```

Il profilo pubblica in `BBDom.Web\bin\Release\net10.0\publish`. La pubblicazione richiede il runtime ASP.NET Core 10 sulla macchina di destinazione.

## Avvio di BBDom.Web

Per avviare in console, dalla radice della soluzione:

```powershell
dotnet run --project .\BBDom.Web\BBDom.Web.csproj -- --console
```

L'avvio dell'app attiva il collegamento KNX configurato. Per l'installazione come servizio Windows, registrare `BBDom.Web.exe` con il Service Control Manager usando il nome `BBDom.Web` e senza `--console`. Se il servizio esiste gia, verificare che punti all'eseguibile della nuova pubblicazione. I comandi `install` e `uninstall` di Topshelf non sono piu supportati.

Per ripristinare lo strumento locale `dotnet-ef`, eseguire `dotnet tool restore` dalla cartella `BBDom.Web`.

## Logging

La sezione `NLog` di `BBDom.Web\appsettings.json` configura il logging su console e in `C:\Log\BBDom.Web\debug.log`, con rotazione giornaliera e fino a 10 archivi. Non serve piu `nlog.config`.
