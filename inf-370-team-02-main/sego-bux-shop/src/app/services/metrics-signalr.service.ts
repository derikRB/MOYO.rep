import { Injectable, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { EventBusService } from './event-bus.service';

function getJwt(): string {
  // MUST match AuthService storage key
  return localStorage.getItem('token') || sessionStorage.getItem('token') || '';
}

@Injectable({ providedIn: 'root' })
export class MetricsSignalRService {
  private hub?: signalR.HubConnection;

  constructor(private zone: NgZone, private bus: EventBusService) {}

  connect() {
    if (this.hub) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/metrics`, {
        accessTokenFactory: () => getJwt()
      })
      .withAutomaticReconnect()
      .build();

    this.hub.onreconnected(() => this.zone.run(() => this.bus.emit('metrics:connected', true)));
    this.hub.onclose(() => this.zone.run(() => this.bus.emit('metrics:connected', false)));

    // Example metric push from server
    this.hub.on('MetricsUpdate', (payload: any) => {
      this.zone.run(() => this.bus.emit('metrics:update', payload));
    });

    this.hub
      .start()
      .then(() => this.zone.run(() => this.bus.emit('metrics:connected', true)))
      .catch(err => console.error('SignalR start error', err));
  }

  disconnect() {
    if (!this.hub) return;
    this.hub.stop().catch(() => {});
    this.hub = undefined;
  }
}
