<#
.SYNOPSIS
    XgateSmsChannel 单元测试 + 覆盖率 + SonarQube 分析一键脚本 (The MSBuild Sandwich)。

.DESCRIPTION
    流程：
      Step A: dotnet sonarscanner begin  (声明读取 OpenCover 覆盖率报告)
      Step B: dotnet build               (编译整个解决方案，被 Sonar 截获分析)
      Step C: dotnet test + coverlet     (执行测试并生成 OpenCover XML)
      Step D: dotnet sonarscanner end    (上传分析结果到 SonarQube)

    说明：覆盖率插桩(coverlet)会改写程序集破坏强名称签名，故本脚本统一以
    Coverage=true 构建，主项目在该模式下不签名（不影响正式 Release 打包）。

.EXAMPLE
    .\run-sonar-coverage.ps1 -SonarToken "sqp_xxx" -SonarHostUrl "http://localhost:9000"
#>
param(
    [Parameter(Mandatory = $true)] [string] $SonarToken,
    [string] $SonarHostUrl  = "http://localhost:9000",
    [string] $ProjectKey    = "XgateSmsChannel",
    [string] $ProjectName   = "XgateSmsChannel",
    [string] $Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

# 以脚本所在目录为根（XgateSmsChannel 目录）
$Root        = $PSScriptRoot
$Solution    = Join-Path $Root "XgateSmsChannel.Plugins\XgateSmsChannel.Plugins.sln"
$TestProject = Join-Path $Root "XgateSmsChannel.Plugins.Tests\XgateSmsChannel.Plugins.Tests.csproj"
# coverlet 输出的 OpenCover 报告（绝对路径，供 Sonar 精确读取）
$CoverageFile = Join-Path $Root "XgateSmsChannel.Plugins.Tests\TestResults\coverage.opencover.xml"

# 清理上一次的覆盖率文件，避免读到旧数据
if (Test-Path $CoverageFile) { Remove-Item $CoverageFile -Force }

Write-Host "==== Step A: sonarscanner begin ====" -ForegroundColor Cyan
dotnet sonarscanner begin `
    /k:"$ProjectKey" `
    /n:"$ProjectName" `
    /d:sonar.host.url="$SonarHostUrl" `
    /d:sonar.login="$SonarToken" `
    /d:sonar.cs.opencover.reportsPaths="$CoverageFile" `
    /d:sonar.coverage.exclusions="**/*.Tests/**,**/*Tests.cs" `
    /d:sonar.exclusions="**/obj/**,**/bin/**"
if ($LASTEXITCODE -ne 0) { throw "sonarscanner begin 失败 (exit $LASTEXITCODE)" }

Write-Host "==== Step B: dotnet build ====" -ForegroundColor Cyan
# Coverage=true 关闭强名称签名，使 coverlet 可对程序集插桩
dotnet build $Solution -c $Configuration /p:Coverage=true
if ($LASTEXITCODE -ne 0) { throw "dotnet build 失败 (exit $LASTEXITCODE)" }

Write-Host "==== Step C: dotnet test + coverlet (OpenCover) ====" -ForegroundColor Cyan
dotnet test $TestProject -c $Configuration --no-restore /p:Coverage=true `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=opencover `
    /p:CoverletOutput="$CoverageFile" `
    /p:Include="[XgateSmsChannel.Plugins]*" `
    /p:Exclude="[XgateSmsChannel.Plugins.Tests]*"
if ($LASTEXITCODE -ne 0) { throw "dotnet test 失败 (exit $LASTEXITCODE)" }

Write-Host "==== Step D: sonarscanner end ====" -ForegroundColor Cyan
dotnet sonarscanner end /d:sonar.login="$SonarToken"
if ($LASTEXITCODE -ne 0) { throw "sonarscanner end 失败 (exit $LASTEXITCODE)" }

Write-Host "完成！请在 SonarQube 查看分析与覆盖率结果：$SonarHostUrl/dashboard?id=$ProjectKey" -ForegroundColor Green
