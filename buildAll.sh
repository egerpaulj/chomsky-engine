for d in */ ; do
    echo "Starting build $d"
    cd $d
    if [ $d != "TestData/" ]; then
        dotnet build
    fi
    
    cd ..
done