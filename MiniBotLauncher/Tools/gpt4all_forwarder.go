package main

import (
    "io"
    "log"
    "net"
)

func main() {
    listener, err := net.Listen("tcp", ":4891")
    if err != nil {
        log.Fatalf("Listen error: %v", err)
    }
    log.Println("Forwarding LAN traffic :4891 → 127.0.0.1:4891")

    for {
        client, err := listener.Accept()
        if err != nil {
            log.Printf("Accept error: %v", err)
            continue
        }

        go func() {
            server, err := net.Dial("tcp", "127.0.0.1:4891")
            if err != nil {
                log.Printf("Connection error: %v", err)
                client.Close()
                return
            }

            go io.Copy(server, client)
            go io.Copy(client, server)
        }()
    }
}
