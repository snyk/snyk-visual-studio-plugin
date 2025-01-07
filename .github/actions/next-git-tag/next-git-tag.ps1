<#
    .SYNOPSIS
    This script calculates the next Git tag in two modes:
    1. SemVer format: Increment the patch version based on the latest Git tag.
    2. Time-based format: Generate a tag based on the current date and time in the format YYYY.MM.DDHH.
#>

[CmdletBinding()]
Param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("semver", "time")]
    [string]$VersionType,  # Parameter to choose the versioning method: "semver" or "time"

    [Parameter(Mandatory = $false)]
    [ValidateSet("patch", "minor", "major")]
    [string]$BumpType = "patch" # Parameter to choose the bump type for semver mode
)

if ($VersionType -eq "semver") {
    # SemVer Mode: Increment patch version of the latest Git tag
    
    [string[]]$AllTags = (git tag --sort=committerdate)
    Write-Verbose "All tags: $AllTags"
    $LatestTag = $AllTags[$AllTags.Length - 1]
    Write-Host "Found latest Git tag: $LatestTag"

    # Split the tag by dots and increment patch version
    $v = $LatestTag -split "\."
    [string]$MajorStr = $v[0]
    [int]$Major = 0
    if ($MajorStr.StartsWith("v")) {
        $Major = $MajorStr.Substring(1)
    } else {
        $Major = $MajorStr
    }
    [int]$Minor = $v[1]
    [int]$Patch = $v[2]

    Write-Verbose "MAJOR part: $Major"
    Write-Verbose "MINOR part: $Minor"
    Write-Verbose "PATCH part: $Patch"

    switch ($BumpType) {
        "major" {
            $Major += 1
            $Minor = 0
            $Patch = 0
        }
        "minor" {
            $Minor += 1
            $Patch = 0
        }
        "patch" {
            $Patch += 1
        }
        default {
            Write-Error "Invalid bump type specified: $BumpType"
            exit 1
        }
    }

    $NextSemverTag = "$Major.$Minor.$Patch"
    Write-Host "Next SemVer tag: $NextSemverTag"

} elseif ($VersionType -eq "time") {
    # Time-based Mode: Generate a tag based on the current time (YYYY.MM.DDHH)

    # Get current date and time in UTC (you can adjust to any timezone as needed)
    $Date = [DateTime]::UtcNow
    Write-Host "Current UTC date and time: $Date"

    # Extract components to form the version string
    $Year = $Date.Year
    $Month = $Date.Month
    $Day = $Date.Day
    $Hour = $Date.Hour

    # Format the version tag as YYYY.MM.DDHH
    $NextSemverTag = "$Year.$Month.$Day$($Hour.ToString().PadLeft(2,'0'))"
    Write-Host "Next SemVer tag based on current time: $NextSemverTag"
}

# Return the next tag to the workflow
Write-Output "::set-output name=next-tag::$NextSemverTag"
