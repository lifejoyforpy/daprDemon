--log-level info --log-as-json

dapr run  -a ServiceA  --dapr-http-port 3520 --app-port 7000  --components-path ./components/ --  dotnet run  --project ./ServiceA