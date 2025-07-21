#start -FilePath "powershell" -WorkingDirectory ".\.Azurite" -ArgumentList "azurite --location .\.Azurite --debug .\.Azurite\debug.log"


start -FilePath "powershell" -ArgumentList "azurite --location .\.Azurite --debug .\.Azurite\debug.log --skipApiVersionCheck"

