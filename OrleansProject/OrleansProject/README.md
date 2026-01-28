Command for starting nats:

```
docker run -d --name nats -p 4222:4222 -p 8222:8222 nats:latest
docker run -d --name redis -p 6379:6379 -v redis-data:/data redis:7 redis-server --appendonly yes
```