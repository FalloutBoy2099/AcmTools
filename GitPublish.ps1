param(
	[String]$Configuration = "Release",
	[ValidateSet("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
	[String[]]$RuntimeIdentifier = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"),
	[String]$OutputDirectory = ""
)

# Builds one framework-dependent single-file executable per runtime (the .NET 10 runtime is NOT embedded,
# it must be installed on the target machine) and packs it as <runtime>.zip, e.g. win-x64.zip.

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = $PSScriptRoot
if ([String]::IsNullOrWhiteSpace($root))
{
	$root = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$project = Join-Path $root "AcmTools.Cli\AcmTools.Cli.csproj"
if (-not (Test-Path -LiteralPath $project))
{
	throw "Project not found: $project"
}

if ([String]::IsNullOrWhiteSpace($OutputDirectory))
{
	$zipRoot = Join-Path $root "publish"
}
else
{
	$zipRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $root $OutputDirectory }
}

$staging = Join-Path $zipRoot "_staging"
New-Item -ItemType Directory -Force -Path $zipRoot | Out-Null

function Publish-Zip
{
	param([String]$Runtime)

	$windowsTarget = $Runtime.StartsWith("win-", [StringComparison]::OrdinalIgnoreCase)
	$extension = if ($windowsTarget) { ".exe" } else { "" }
	$baseName = if ($Runtime.StartsWith("linux-", [StringComparison]::OrdinalIgnoreCase)) { "acmconverter" } else { "AcmConverter" }
	$executableName = "$baseName$extension"
	$publishDirectory = Join-Path $staging $Runtime
	$zipPath = Join-Path $zipRoot "$Runtime.zip"

	Write-Host "[$Runtime] Publishing framework-dependent single-file executable (without the .NET runtime)..."
	$arguments = @(
		"publish", $project,
		"--configuration", $Configuration,
		"--runtime", $Runtime,
		"--self-contained", "false",
		"-p:PublishSingleFile=true",
		"-p:IncludeNativeLibrariesForSelfExtract=true",
		"-p:DebugType=None",
		"-p:DebugSymbols=false",
		"-p:PublishDir=$publishDirectory$([IO.Path]::DirectorySeparatorChar)"
	)

	& dotnet @arguments | Out-Host
	if ($LASTEXITCODE -ne 0)
	{
		throw "dotnet publish failed for $Runtime with exit code $LASTEXITCODE."
	}

	if (Test-Path -LiteralPath $zipPath)
	{
		Remove-Item -LiteralPath $zipPath -Force
	}

	$archive = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
	try
	{
		$entry = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
			$archive,
			(Join-Path $publishDirectory "AcmTools.Cli$extension"),
			$executableName,
			[IO.Compression.CompressionLevel]::Optimal)

		if (-not $windowsTarget)
		{
			# Unix mode 0755 (rwxr-xr-x) so the extracted file is directly executable on Linux and macOS.
			$entry.ExternalAttributes = (0x81ED -shl 16)
		}
	}
	finally
	{
		$archive.Dispose()
	}

	Get-Item -LiteralPath $zipPath
}

try
{
	$results = foreach ($runtime in ($RuntimeIdentifier | Select-Object -Unique))
	{
		Publish-Zip -Runtime $runtime
	}
}
finally
{
	if (Test-Path -LiteralPath $staging)
	{
		Remove-Item -LiteralPath $staging -Recurse -Force
	}
}

Write-Host ""
Write-Host "Packages ready:"
$results | Select-Object Name, Length, FullName | Format-Table -AutoSize