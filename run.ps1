if (!($args.Count -eq 0)) {
    dotnet run
    exit
}
dotnet run --no-build
