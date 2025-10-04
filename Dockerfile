FROM public.ecr.aws/lambda/dotnet:9

WORKDIR /var/task

COPY src/ManaFood.AuthLambda/bin/Release/net9.0/linux-x64/publish/ .

CMD ["bootstrap::ManaFood.AuthLambda.Function::FunctionHandler"]