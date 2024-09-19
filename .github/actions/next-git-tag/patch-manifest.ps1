<#
    .SYNOPSIS
    This script modifies the extension.vsixmanifest file by appending "preview" to the DisplayName when it's a release.
#>

Param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,  # Path to extension.vsixmanifest
    [Parameter(Mandatory = $true)]
    [string]$PipelineType,   # Type of pipeline: "preview" or "stable"
    [Parameter(Mandatory = $true)]
    [string]$Version   # Extension Version
)

# Load the XML file
[xml]$xml = Get-Content -Path $ManifestPath

# Check the pipeline type, and if it's a release, modify the DisplayName
if ($PipelineType -eq "preview") {
    Write-Host "Pipeline is release, setting 'preview' to DisplayName."
    $displayNameNode = $xml.PackageManifest.Metadata.DisplayName
    # Append 'preview' to the existing DisplayName
    $displayNameNode = "(Preview) $displayNameNode"
    $xml.PackageManifest.Metadata.DisplayName = $displayNameNode
    $xml.PackageManifest.Metadata.Preview = "true"
    $xml.PackageManifest.Metadata.Identity.SetAttribute("Id", "snyk_visual_studio_plugin_2022_preview.27b810bb-3e15-4b77-8866-e8ea515a6ee6")
    Write-Host "Updated DisplayName: $displayNameNode"
    Write-Host "Updated Preview: true"
    # Save the modified XML back to the file
}

$xml.PackageManifest.Metadata.Identity.SetAttribute("Version", $Version)
Write-Host "Updated Version to: $Version"
$xml.Save($ManifestPath)
Write-Host "Manifest file has been updated successfully."