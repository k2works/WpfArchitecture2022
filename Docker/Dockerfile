FROM mcr.microsoft.com/mssql/server:2022-latest

# Install mssql-tools
USER root
RUN apt-get update && apt-get install -y curl apt-transport-https gnupg lsb-release \
    && curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - \
    && curl https://packages.microsoft.com/config/ubuntu/22.04/prod.list > /etc/apt/sources.list.d/msprod.list \
    && apt-get update \
    && ACCEPT_EULA=Y apt-get install -y mssql-tools unixodbc-dev \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Add sqlcmd to PATH
ENV PATH="$PATH:/opt/mssql-tools/bin"

USER mssql
WORKDIR /work/

COPY build/ .

RUN /bin/bash ./setup.sh

ENTRYPOINT ["/opt/mssql/bin/sqlservr"]
