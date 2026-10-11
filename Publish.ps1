param(
	[String]$Configuration = "Release",
	[ValidateSet("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
	[String[]]$RuntimeIdentifier = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"),
	[String]$OutputDirectory = "",
	[Switch]$Clean
)

$ErrorActionPreference = "Stop"

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
	$publishRoot = Join-Path $root "publish"
}
else
{
	$publishRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $root $OutputDirectory }
}

function Publish-Executable
{
	param(
		[String]$Runtime,
		[Boolean]$SelfContained,
		[String]$PublishDirectory
	)

	$selfContainedValue = if ($SelfContained) { "true" } else { "false" }
	$arguments = @(
		"publish", $project,
		"--configuration", $Configuration,
		"--runtime", $Runtime,
		"--self-contained", $selfContainedValue,
		"-p:PublishSingleFile=true",
		"-p:IncludeNativeLibrariesForSelfExtract=true",
		"-p:DebugType=None",
		"-p:DebugSymbols=false",
		"-p:PublishDir=$PublishDirectory$([IO.Path]::DirectorySeparatorChar)"
	)

	if ($SelfContained)
	{
		$arguments += "-p:EnableCompressionInSingleFile=true"
	}

	& dotnet @arguments | Out-Host
	if ($LASTEXITCODE -ne 0)
	{
		throw "dotnet publish failed for $Runtime with exit code $LASTEXITCODE."
	}
}

function Publish-Runtime
{
	param([String]$Runtime)

	$output = Join-Path $publishRoot $Runtime
	$staging = Join-Path $output "_staging"
	$frameworkStaging = Join-Path $staging "framework-dependent"
	$selfContainedStaging = Join-Path $staging "self-contained"
	$frameworkOutput = Join-Path $output "without-dotnet"
	$selfContainedOutput = Join-Path $output "with-dotnet"
	$executableExtension = if ($Runtime.StartsWith("win-", [StringComparison]::OrdinalIgnoreCase)) { ".exe" } else { "" }
	$executableBaseName = if ($Runtime.StartsWith("linux-", [StringComparison]::OrdinalIgnoreCase)) { "acmconverter" } else { "AcmConverter" }
	$executableName = "$executableBaseName$executableExtension"

	if ($Clean -and (Test-Path -LiteralPath $output))
	{
		Remove-Item -LiteralPath $output -Recurse -Force
	}

	New-Item -ItemType Directory -Force -Path $output, $frameworkOutput, $selfContainedOutput | Out-Null
	if (Test-Path -LiteralPath $staging)
	{
		Remove-Item -LiteralPath $staging -Recurse -Force
	}

	Write-Host "[$Runtime] Publishing framework-dependent single-file executable..."
	Publish-Executable -Runtime $Runtime -SelfContained $false -PublishDirectory $frameworkStaging

	Write-Host "[$Runtime] Publishing self-contained single-file executable with .NET 10 runtime..."
	Publish-Executable -Runtime $Runtime -SelfContained $true -PublishDirectory $selfContainedStaging

	$frameworkTarget = Join-Path $frameworkOutput $executableName
	$selfContainedTarget = Join-Path $selfContainedOutput $executableName
	Copy-Item -LiteralPath (Join-Path $frameworkStaging "AcmTools.Cli$executableExtension") -Destination $frameworkTarget -Force
	Copy-Item -LiteralPath (Join-Path $selfContainedStaging "AcmTools.Cli$executableExtension") -Destination $selfContainedTarget -Force
	Remove-Item -LiteralPath $staging -Recurse -Force

	Get-Item -LiteralPath $frameworkTarget, $selfContainedTarget
}

$results = foreach ($runtime in ($RuntimeIdentifier | Select-Object -Unique))
{
	Publish-Runtime -Runtime $runtime
}

Write-Host ""
Write-Host "Publish complete:"
$results | Select-Object Name, Length, FullName | Format-Table -AutoSize
