<#
    .SYNOPSIS
    This script calculates a next Git tag in SemVer format.
#>
[CmdletBinding()]
Param()

[string[]]$AllTags = (git tag --sort=committerdate)
Write-Verbose "all tags: $AllTags"
$LatestTag = $AllTags[$AllTags.Length - 1]
Write-Host "Found latest Git tag: $LatestTag"

# split the tag by dots and increment patch version
$v = $LatestTag -split "\."
[string] $MajorStr = $v[0]
[int] $Major = 0
if ($MajorStr.StartsWith("v"))
{
    $Major = $MajorStr.Substring(1)
}
else
{
    $Major = $MajorStr
}
[int] $Minor = $v[1]
[int] $Patch = $v[2]

Write-Verbose "MAJOR part: $Major"
Write-Verbose "MINOR part: $Minor"
Write-Verbose "PATCH part: $Patch"

# hardcoded increment for patch
$Patch += 1;

$NextSemverTag = "$Major.$Minor.$Patch"
Write-Host "Next SemVer tag: $NextSemverTag"

# Return next tag to workflow
echo "::set-output name=next-tag::$NextSemverTag"
