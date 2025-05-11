// gpt4all_forwarder.go

// This program forwards all incoming network traffic on port 4891
// from any device on your LAN to the GPT4All server running locally
// on this same computer at 127.0.0.1:4891 (localhost).
// This allows other devices to access GPT4All as if it were shared on the network.

package main

import (
    "io"      // for copying data between connections
    "log"     // for printing status messages to the console
    "net"     // for network (TCP) operations
)

func main() {
    // Start a TCP listener on all interfaces, port 4891.
    // This means it listens for LAN connections on 192.168.x.x:4891
    listener, err := net.Listen("tcp", ":4891")
    if err != nil {
        log.Fatalf("Listen error: %v", err) // Exit if it fails to bind
    }

    // Inform the user that it's running
    log.Println("Forwarding LAN traffic :4891 → 127.0.0.1:4891")

    // Infinite loop: accept and handle incoming connections
    for {
        // Accept a new client (incoming LAN connection)
        client, err := listener.Accept()
        if err != nil {
            log.Printf("Accept error: %v", err)
            continue // skip this connection and wait for the next
        }

        // Handle each connection in a separate "goroutine" (like a thread)
        go func() {
            // Try to connect to the local GPT4All server (still on this PC)
            server, err := net.Dial("tcp", "127.0.0.1:4891")
            if err != nil {
                log.Printf("Connection error: %v", err)
                client.Close()
                return
            }

            // Copy data from client → GPT4All
            go io.Copy(server, client)
            // Copy data from GPT4All → client
            go io.Copy(client, server)

            // Once both copies are done, the connection will close automatically
        }()
    }
}
