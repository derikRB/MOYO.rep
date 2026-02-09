import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateFeedbackDto, FeedbackDto } from '../dto/feedback.dto';

@Injectable({ providedIn: 'root' })
export class FeedbackService {
  private baseUrl = `${environment.apiUrl}/api/Feedback`;

  constructor(private http: HttpClient) {}

  submitFeedback(dto: CreateFeedbackDto): Observable<FeedbackDto> {
    return this.http.post<FeedbackDto>(this.baseUrl, dto);
  }

  getMine(): Observable<FeedbackDto[]> {
    return this.http.get<FeedbackDto[]>(`${this.baseUrl}/my`);
  }

  // Admin/Employee/Manager view
  getAll(): Observable<FeedbackDto[]> {
    return this.http.get<FeedbackDto[]>(`${this.baseUrl}/all`);
  }

  // NEW: feedback associated with orders that include a given product
  getForProduct(productId: number): Observable<FeedbackDto[]> {
    return this.http.get<FeedbackDto[]>(`${this.baseUrl}/product/${productId}`);
  }
}
