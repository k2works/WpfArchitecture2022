# Docker セットアップガイド

## 初回セットアップ

このプロジェクトを初めてクローンした場合の手順：

### 1. 前提条件
- Docker Desktop がインストール済みであること
- Git がインストール済みであること

### 2. データベースの起動

```bash
# プロジェクトルートディレクトリで実行
docker-compose -f compose-dev.yaml up -d --build
```

初回実行時は以下の処理が行われます：
- SQL Server 2022 のダウンロード
- mssql-tools のインストール
- AdventureWorks データベースの作成とデータ投入
- 再購買システムのテーブル・ビュー作成

### 3. 接続確認

数分待ってから以下のコマンドでデータベースが正常に作成されているか確認：

```bash
docker exec SQL-Server bash -c "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P P@ssw0rd! -Q 'SELECT name FROM sys.databases'"
```

以下のデータベースが表示されれば成功：
- master
- tempdb
- model
- msdb
- AdventureWorks

### 4. 接続文字列

アプリケーションから以下の接続文字列を使用：

```
Server=localhost,1433;Database=AdventureWorks;User Id=sa;Password=P@ssw0rd!;TrustServerCertificate=True;
```

アプリケーション用ユーザー（推奨）：
```
Server=localhost,1433;Database=AdventureWorks;User Id=AdventureWorks;Password=xR^g*BV2XX8d2p77;TrustServerCertificate=True;
```

## 日常的な操作

### データベースの停止
```bash
docker-compose -f compose-dev.yaml down
```

### データベースの起動（2回目以降）
```bash
docker-compose -f compose-dev.yaml up -d
```

### データベースの完全リセット
```bash
docker-compose -f compose-dev.yaml down
docker image rm wpfarchitecture2022-sqlserver:latest
docker-compose -f compose-dev.yaml up -d --build
```

## トラブルシューティング

### ポート 1433 が使用中の場合
他の SQL Server インスタンスが起動している可能性があります。
```bash
# 実行中のコンテナを確認
docker ps

# 他の SQL Server を停止
docker stop <container-name>
```

### データが表示されない場合
```bash
# コンテナログを確認
docker logs SQL-Server

# vProductRequiringPurchase ビューのデータ確認
docker exec SQL-Server bash -c "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P P@ssw0rd! -d AdventureWorks -Q 'SELECT COUNT(*) FROM RePurchasing.vProductRequiringPurchase'"
```

正常な場合は 6 件のデータが表示されます。