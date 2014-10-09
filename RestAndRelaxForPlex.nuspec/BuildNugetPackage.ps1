del *.nupkg; 
nuget pack .\RestAndRelaxForPlex.nuspec\JimBobBennett.RestAndRelaxForPlex.nuspec -Symbols -Properties Configuration=Debug;
cp *.nupkg c:\NuGet