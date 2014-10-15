del *.nupkg; 
nuget pack .\RestAndRelaxForPlex.nuspec\JimBobBennett.RestAndRelaxForPlex.nuspec -Symbols -Properties Configuration=Release;
cp *.nupkg c:\NuGet