import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Customer {
  id: number;
  username: string;
  name: string;
  surname: string;
  email: string;
  phone: string;
  address: string;
}

export interface UpdateCustomerDto {
  username: string;
  name: string;
  surname: string;
  email: string;
  phone: string;
  address: string;
}

@Injectable({
  providedIn: 'root'
})
export class CustomerService {
  private baseUrl = 'https://localhost:7025/api/Customer';

  constructor(private http: HttpClient) {}

  getCustomerById(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.baseUrl}/${id}`);
  }

  updateCustomer(id: number, dto: UpdateCustomerDto): Observable<Customer> {
    return this.http.put<Customer>(`${this.baseUrl}/${id}`, dto);
  }

updatePassword(id: number, data: { 
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}): Observable<any> {
  return this.http.put(`${this.baseUrl}/${id}/update-password`, {
    currentPassword: data.currentPassword,
    newPassword: data.newPassword,
    confirmNewPassword: data.confirmNewPassword
  });
}



  deleteCustomer(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }
}
