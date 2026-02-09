import { HttpClient } from '@angular/common/http';
import { Injectable }    from '@angular/core';
import { Observable }    from 'rxjs';

export interface CustomizationDto {
  orderLineID:       number;
  template:          string;
  customText:        string;
  font:              string;
  fontSize:          number;
  color:             string;
  uploadedImagePath: string;
}

@Injectable({ providedIn: 'root' })
export class CustomizationService {
  private base = '/api/Customization';
  constructor(private http: HttpClient) {}

  get(orderLineID: number): Observable<CustomizationDto> {
    return this.http.get<CustomizationDto>(`${this.base}/${orderLineID}`);
  }
  create(dto: CustomizationDto): Observable<CustomizationDto> {
    return this.http.post<CustomizationDto>(this.base, dto);
  }
  update(orderLineID: number, dto: CustomizationDto): Observable<CustomizationDto> {
    return this.http.put<CustomizationDto>(`${this.base}/${orderLineID}`, dto);
  }
  delete(orderLineID: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${orderLineID}`);
  }
}
