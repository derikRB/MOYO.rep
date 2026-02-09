import { Injectable } from '@angular/core';
import { HttpClient }  from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap }         from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface Vat {
  vatId: number;
  vatName: string;
  percentage: number;
  effectiveDate: string;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class VatService {
  private baseUrl = `${environment.apiUrl}/api/vat`;

  private vatRateSubject = new BehaviorSubject<number>(0);
  vatRate$ = this.vatRateSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadActiveRate().subscribe();
  }

  public get vatRateValue(): number {
    return this.vatRateSubject.getValue();
  }

  loadActiveRate(): Observable<Vat> {
    return this.http
      .get<Vat>(`${this.baseUrl}/active`)
      .pipe(tap(v => this.vatRateSubject.next(v.percentage)));
  }

  getAll(): Observable<Vat[]> {
    return this.http.get<Vat[]>(this.baseUrl);
  }

  create(dto: Partial<Vat>): Observable<Vat> {
    return this.http
      .post<Vat>(this.baseUrl, dto)
      .pipe(tap(() => this.loadActiveRate().subscribe()));
  }

  activate(id: number): Observable<Vat> {
    return this.http
      .post<Vat>(`${this.baseUrl}/${id}/activate`, {})
      .pipe(tap(() => this.loadActiveRate().subscribe()));
  }
}
