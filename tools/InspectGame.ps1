param([string]$TypePattern, [string]$MethodPattern = ".", [switch]$NamesOnly, [string]$Calls)
$ErrorActionPreference = "Stop"
$cecil = Get-ChildItem "$env:USERPROFILE/.nuget/packages/mono.cecil" -Recurse -Filter Mono.Cecil.dll | Where-Object FullName -Match "net40" | Select-Object -First 1
if (!$cecil) { throw "Mono.Cecil is required in the local NuGet cache." }
Add-Type -Path $cecil.FullName
$managed = "C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data/Managed"
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($managed)
$parameters = New-Object Mono.Cecil.ReaderParameters
$parameters.AssemblyResolver = $resolver
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$managed/RoR2.dll", $parameters)
function Show-Type($type) {
    if ($Calls) {
        foreach ($method in $type.Methods) {
            if (!$method.HasBody) { continue }
            $matches = @($method.Body.Instructions | Where-Object { $_.Operand -and $_.Operand.ToString() -match $Calls })
            if ($matches.Count) {
                "METHOD $($method.FullName)"
                foreach ($instruction in $matches) { " $instruction" }
            }
        }
        foreach ($nested in $type.NestedTypes) { Show-Type $nested }
        return
    }
    if ($type.FullName -match $TypePattern) {
        "TYPE $($type.FullName)"
        if (!$NamesOnly) {
            foreach ($field in $type.Fields) { " FIELD $($field.FieldType) $($field.Name)" }
            foreach ($method in $type.Methods | Where-Object Name -Match $MethodPattern) {
                " METHOD $($method.FullName)"
                if ($method.HasBody) { foreach ($instruction in $method.Body.Instructions) { "  $instruction" } }
            }
        }
    }
    foreach ($nested in $type.NestedTypes) { Show-Type $nested }
}
try { foreach ($type in $assembly.MainModule.Types) { Show-Type $type } }
finally { $assembly.Dispose(); $resolver.Dispose() }
