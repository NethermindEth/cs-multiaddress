# Multiformats Multiaddr

[![Nethermind.Multiformats.Address](https://img.shields.io/nuget/v/Nethermind.Multiformats.Address)](https://www.nuget.org/packages/Nethermind.Multiformats.Address)

[Multiaddr](https://github.com/multiformats/multiaddr) implementation in C#.

## Table of Contents

- [Install](#install)
- [Usage](#usage)
- [Supported protocols](#supported-protocols)
- [Maintainers](#maintainers)
- [Contribute](#contribute)
- [License](#license)

## Install

    PM> Install-Package Multiformats.Address

---

    dotnet add package Multiformats.Address

## Usage
``` cs
var ma = Multiaddress.Decode("/ip4/127.0.0.1/udp/1234");
var addresses = ma.Split();
var joined = Multiaddress.Join(addresses);
var tcp = ma.Protocols.Get<TCP>();
```

There's some extension methods included that let's you create multiaddresses of IPEndPoints, and create IPEndPoints from multiaddresses.
Some let's you create sockets directly from IP4/IP6, TCP/UDP multiaddresses.

``` cs
var socket = ma.CreateSocket();
var localEndPoint = socket.GetLocalMultiaddress();
var remoteEndPoint = socket.GetRemoteMultiaddress();
```

## Supported protocols

* DCCP
* DNS/4/6
* HTTP
* HTTPS
* IPv4
* IPv6
* IPFS (deprecated - use P2P)
* Onion
* P2P
* SCTP
* TCP
* UDP
* UDT
* Unix
* WebRTCDirect
* WebRTCStar
* WebSocket
* WebSocket Secure
