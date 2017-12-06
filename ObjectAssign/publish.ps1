param (
    [switch]$pack = $false,
    [switch]$publish = $false,
    [switch]$all = $false,
    [string]$NugetsFolder ="./nugets",
    [string]$NugetServer ="\\192.168.1.70\keanugets"
)

"*******************"
"Kea Nugets"
"*******************"

if($all) {
    $pack = $true
    $publish = $true
}
if (-Not ($pack -Or $publish ) ) {

    "-Pack: Generar el archivo nuget, y ponerlo en la carpeta /nugets"
    "-Publish: Publicar el nugets mas reciente de la carpeta /nugets"
    "-All: Igual a correr -Pack seguido de -Publish"
    "-NugetsFolder: Directorio donde estan los nugets, por default es ./nugets"
    "-NugetServer: Directorio donde se deben de publicar los nugets"
	"Para automatizar pegue la siguiente linea en Post-build event command line"
	"cd ../../ && powershell -ExecutionPolicy bypass -File ""publish.ps1"" ""-All"""
}

$projName = (Get-ChildItem *.csproj |
Select-Object -ExpandProperty BaseName -First 1)

"Nombre de proyecto: " + $projName

#Obtiene el nuget mas reciente de una carpeta o null
function Get-LastNuget {
    param ([string]$path=".")
    
    Push-Location $path

    $ret = Get-ChildItem -Path ($projName + "*.nupkg") |
    Sort-Object -Property LastWriteTime -Descending |
    Select-Object -ExpandProperty Name -First 1

    Pop-Location
    if($ret -eq "") {
        $null
    } else {
        Join-Path -Path $path -ChildPath $ret
    }
}

if($path -Or $publish) {
    #Creamos el directorio de los nugets si no existe
    if(-Not (Test-Path $NugetsFolder)) {
        "Creando el directorio de nugets "
        mkdir $NugetsFolder
    }
}

if($pack) {
    nuget pack
    $lastNuget = Get-LastNuget
    "El ultimo nuget es: " + $lastNuget
    Move-Item -Path $lastNuget -Destination $nugetsFolder
}

if($publish) {
    $lastNuget = Get-LastNuget $nugetsFolder
    "Publicando " + $lastNuget
    nuget add $lastNuget -source $NugetServer
    "Listo"
}

