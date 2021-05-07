# Heizung.DataReciever

## Übersicht

> Deises Projekt ist noch nicht komplet abgeschlossen. Es Funktioniert also noch nicht.

Dieses Projekt ermöglicht es Statuswerte aus der Heizung Fröhling S3 Turbo auszulesen und an eine WebApi weiterzugeben. (Wurde nur mit "Fröhling S3 Turbo" getestet. Es ist möglich, das andere Modelle von Fröhling diese Daten auch ausgeben)

Die Daten werden über die COM2 Schnittstelle der Heizung ausgelesen, welche sich unter dem Deckel der Heizung befindet.

Die Daten werden von der Heizung alle paar Sekunden über den COM-Port gesendet. Das Projekt liest diese aus und überprüft, ob die Daten sich geändert haben und senden diese dann an die WebApi weiter.

## Benutzer der Software

Damit die Software funtkioniert, muss in der Konfigdatei (appsettings.json) die WebApi und der COM-Port angegeben werden.