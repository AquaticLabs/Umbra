param([string]$Managed = "C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data/Managed")
$ErrorActionPreference = "Stop"
$cecil = Get-ChildItem "$env:USERPROFILE/.nuget/packages/mono.cecil" -Recurse -Filter Mono.Cecil.dll | Where-Object FullName -Match "net40" | Select-Object -First 1
if (!$cecil) { throw "Mono.Cecil is required in the local NuGet cache." }
Add-Type -Path $cecil.FullName
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$Managed/RoR2.dll")
function Require-Method($typeName, $methodName, $parameterCount) {
    $type = $assembly.MainModule.Types | Where-Object FullName -EQ $typeName
    $method = @($type.Methods | Where-Object { $_.Name -eq $methodName -and $_.Parameters.Count -eq $parameterCount })
    if ($method.Count -ne 1) { throw "Expected exactly one $typeName.$methodName with $parameterCount parameters." }
    Write-Host "PASS $typeName.$methodName"
    return $method[0]
}
try {
    $afford = Require-Method "RoR2.CostTypeDef" "IsAffordable" 2
    if ($afford.Parameters[1].ParameterType.FullName -ne "RoR2.Interactor") { throw "Affordability interactor signature changed." }
    $pay = Require-Method "RoR2.CostTypeDef" "PayCost" 2
    if ($pay.Parameters[0].ParameterType.FullName -ne "RoR2.CostTypeDef/PayCostContext") { throw "Payment context signature changed." }
    $null = Require-Method "RoR2.CharacterMaster" "GetDeployableSameSlotLimit" 1
    $null = Require-Method "RoR2.CharacterBody" "RecalculateStats" 0
    $null = Require-Method "RoR2.CharacterBody" "set_moveSpeed" 1
    $null = Require-Method "RoR2.CharacterBody" "set_maxJumpCount" 1
    $null = Require-Method "RoR2.ChestBehavior" "Roll" 0
    $drop = Require-Method "RoR2.ChestBehavior" "BaseItemDrop" 0
    if (!($drop.Body.Instructions | Where-Object { $_.Operand -and $_.Operand.ToString().Contains("get_currentPickup") })) { throw "Chest drop no longer reads currentPickup." }
    $null = Require-Method "RoR2.ChestBehavior" "set_currentPickup" 1
    $null = Require-Method "RoR2.ShopTerminalBehavior" "CurrentPickup" 0
    Write-Host "Game contracts passed. This checks installed assembly signatures, not live Harmony/network behavior."
}
finally { $assembly.Dispose() }
