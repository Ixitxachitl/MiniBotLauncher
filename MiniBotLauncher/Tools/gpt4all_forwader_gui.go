// Default themed version
package main

import (
	"bytes"
	"fmt"
	"image/png"
	"io"
	"net"
	"strconv"
	"sync"
	"syscall"
	"unsafe"

	_ "embed"

	"github.com/lxn/walk"
)

//go:embed icon.png
var iconPNG []byte

var (
	listener   net.Listener
	proxyWG    sync.WaitGroup
	running    bool
	runningMux sync.Mutex
)

func startProxy(port int, log func(string)) error {
	ln, err := net.Listen("tcp", fmt.Sprintf("0.0.0.0:%d", port))
	if err != nil {
		return err
	}
	listener = ln
	log(fmt.Sprintf("Listening on port %d...", port))
	runningMux.Lock()
	running = true
	runningMux.Unlock()

	proxyWG.Add(1)
	go func() {
		defer proxyWG.Done()
		for {
			runningMux.Lock()
			if !running {
				runningMux.Unlock()
				return
			}
			runningMux.Unlock()

			client, err := ln.Accept()
			if err != nil {
				if !running {
					return
				}
				log(fmt.Sprintf("Accept error: %v", err))
				continue
			}

			go func() {
				server, err := net.Dial("tcp", fmt.Sprintf("127.0.0.1:%d", port))
				if err != nil {
					log(fmt.Sprintf("Connection error: %v", err))
					client.Close()
					return
				}
				go io.Copy(server, client)
				go io.Copy(client, server)
			}()
		}
	}()
	return nil
}

func stopProxy(log func(string)) {
	runningMux.Lock()
	running = false
	runningMux.Unlock()
	if listener != nil {
		_ = listener.Close()
	}
	proxyWG.Wait()
	log("Proxy stopped.")
}

func main() {
	mutexName, _ := syscall.UTF16PtrFromString("Global\\GPT4AllForwarderMutex")
	handle, _, err := syscall.NewLazyDLL("kernel32.dll").NewProc("CreateMutexW").Call(0, 0, uintptr(unsafe.Pointer(mutexName)))
	if handle == 0 {
		fmt.Println("Failed to create mutex")
		return
	}
	if err.(syscall.Errno) == syscall.ERROR_ALREADY_EXISTS {
		walk.MsgBox(nil, "Notice", "Another instance is already running.", walk.MsgBoxIconInformation)
		return
	}
	defer syscall.NewLazyDLL("kernel32.dll").NewProc("CloseHandle").Call(handle)

	mw, err := walk.NewMainWindow()
	if err != nil {
		fmt.Println("Failed to create main window:", err)
		return
	}

	mw.SetTitle("GPT4All Forwarder")
	mw.SetSize(walk.Size{Width: 220, Height: 100})
	mw.SetMinMaxSize(walk.Size{Width: 220, Height: 100}, walk.Size{Width: 220, Height: 100})
	mw.SetLayout(walk.NewVBoxLayout())

	hbox, _ := walk.NewComposite(mw)
	hbox.SetLayout(walk.NewHBoxLayout())

	portLabel, _ := walk.NewLabel(hbox)
	portLabel.SetText("Port:")

	portInput, _ := walk.NewLineEdit(hbox)
	portInput.SetText("4891")
	portInput.SetMinMaxSize(walk.Size{Width: 100, Height: 0}, walk.Size{})

	logText, _ := walk.NewLabel(mw)
	logText.SetText("Idle")

	startBtn, _ := walk.NewPushButton(hbox)
	startBtn.SetText("Start")
	startBtn.Clicked().Attach(func() {
		port, err := strconv.Atoi(portInput.Text())
		if err != nil || port <= 0 || port > 65535 {
			logText.SetText("Invalid port")
			return
		}
		runningMux.Lock()
		on := running
		runningMux.Unlock()
		if on {
			stopProxy(func(msg string) { logText.SetText(msg) })
			startBtn.SetText("Start")
			portInput.SetEnabled(true)
		} else {
			err := startProxy(port, func(msg string) { logText.SetText(msg) })
			if err != nil {
				logText.SetText("Error: " + err.Error())
				return
			}
			startBtn.SetText("Stop")
			portInput.SetEnabled(false)
		}
	})

	mw.Closing().Attach(func(canceled *bool, reason walk.CloseReason) {
		mw.Hide()
		*canceled = true
	})

	notifyIcon, _ := walk.NewNotifyIcon(mw)

	img, err := png.Decode(bytes.NewReader(iconPNG))
	if err != nil {
		walk.MsgBox(nil, "Error", "Failed to decode PNG: "+err.Error(), walk.MsgBoxIconError)
		return
	}

	bmp, err := walk.NewBitmapFromImage(img)
	if err != nil {
		walk.MsgBox(nil, "Error", "Failed to create bitmap: "+err.Error(), walk.MsgBoxIconError)
		return
	}

	icon, err := walk.NewIconFromBitmap(bmp)
	if err != nil {
		walk.MsgBox(nil, "Error", "Failed to create icon: "+err.Error(), walk.MsgBoxIconError)
		return
	}

	mw.SetIcon(icon)
	notifyIcon.SetIcon(icon)
	notifyIcon.SetToolTip("GPT4All Forwarder")
	notifyIcon.SetVisible(true)
	notifyIcon.MouseDown().Attach(func(x, y int, button walk.MouseButton) {
		if button == walk.LeftButton {
			mw.Show()
			mw.Activate()
		}
	})

	showAction := walk.NewAction()
	showAction.SetText("Show")
	showAction.Triggered().Attach(func() {
		mw.Show()
		mw.Activate()
	})
	notifyIcon.ContextMenu().Actions().Add(showAction)

	quitAction := walk.NewAction()
	quitAction.SetText("Quit")
	quitAction.Triggered().Attach(func() {
		stopProxy(func(string) {})
		notifyIcon.Dispose()
		walk.App().Exit(0)
	})
	notifyIcon.ContextMenu().Actions().Add(quitAction)

	mw.Show()
	mw.Run()
}
