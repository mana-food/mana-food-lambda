# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar todo o código fonte
COPY . .

# Build e publicação
RUN dotnet publish src/ManaFood.AuthLambda/ManaFood.AuthLambda.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o /app/publish

# Stage 2: Runtime
FROM public.ecr.aws/lambda/dotnet:9
WORKDIR /var/task

# Copiar apenas os binários publicados do stage anterior
COPY --from=build /app/publish .

CMD ["bootstrap::ManaFood.AuthLambda.Function::FunctionHandler"]