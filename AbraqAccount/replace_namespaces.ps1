param (
    [string]$folder,
    [string]$oldText,
    [string]$newText
)

Get-ChildItem -Path $folder -Filter *.cs -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $newContent = $content -replace $oldText, $newText
    if ($newContent -ne $content) {
        Set-Content $_.FullName $newContent
        Write-Host "Updated $($_.FullName)"
    }
}
