﻿<# 
This script runs all the unit tests specified in `.\tests.nunit` file.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindNunit3Console, `
    FindOpenCoverConsole, `
    CreateAndGetArtefactsDir, `
    FindReportGenerator

function Main
{
    $nunit3Console = FindNunit3Console
    $openCoverConsole = FindOpenCoverConsole
    $reportGenerator = FindReportGenerator

    Set-Location $PSScriptRoot
    $nunitProjectPath = Join-Path $PSScriptRoot "tests.nunit"

    Write-Host "Running the tests specified in: $nunitProjectPath"

    $artefactsDir = CreateAndGetArtefactsDir

    $testResultsPath = Join-Path $artefactsDir "TestResults.xml"
    $coverageResultsPath = Join-Path $artefactsDir "CoverageResults.xml"

    $targetDir = Join-Path $artefactsDir "build\Debug"

    # Relative to $targetDir
    $testDlls = @( "AasxCsharpLibrary.Tests.dll" )

    & $openCoverConsole `
        -target:$nunit3Console `
        -targetargs:( `
            "--noheader --shadowcopy=false " + `
            "--result=$testResultsPath " + `
            ($testDlls -Join " ")
        ) `
        -targetdir:$targetDir `
        -output:$coverageResultsPath `
        -register:user `
        -filter:"+[Aasx*]*"

    if(!$?) {
        throw "The unit test(s) failed."
    }

    # Scripts are expected at the root of src/
    $srcDir = $PSScriptRoot

    $coverageReportPath = Join-Path $artefactsDir "CoverageReport"
    & $reportGenerator `
        -reports:$coverageResultsPath `
        -targetdir:$coverageReportPath `
        -sourcedirs:$srcDir
}

Main
