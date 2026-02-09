import { Component, OnInit }             from '@angular/core';
import { CommonModule }                  from '@angular/common';
import { FormsModule, NgForm }           from '@angular/forms';
import { AdminService, Employee }        from '../../services/admin.service';
import { PaginatePipe }                  from '../../shared/pagination/pagination.pipe';
import { PaginationComponent }           from '../../shared/pagination/pagination.component';
import { ConfirmDialogComponent }        from '../../shared/confirm-dialog.component';
import { ToastService }                  from '../../shared/toast.service';

@Component({
  selector:    'app-manage-employees',
  standalone:  true,
  imports:     [
    CommonModule,
    FormsModule,
    PaginatePipe,
    PaginationComponent,
    ConfirmDialogComponent
  ],
  templateUrl: './manage-employees.component.html',
  styleUrls:   ['./manage-employees.component.scss']
})
export class ManageEmployeesComponent implements OnInit {
  employees: Employee[] = [];
  searchTerm = '';

  form = {
    emailOrUsername: '',
    password:        ''
  };

  editForm: Employee = {
    employeeID: 0,
    username:   '',
    email:      '',
    role:       ''
  };

  editing = false;

  // ─── validation state ──────────────────────────────
  empSubmitted  = false;
  editSubmitted = false;

  // ─── delete confirmation ───────────────────────────
  confirmingDelete     = false;
  toDeleteEmployee: Employee | null = null;

  // ─── pagination ───────────────────────────────────
  page     = 1;
  pageSize = 7;
  get totalPages() {
    return Math.ceil(this.employees.length / this.pageSize) || 1;
  }

  constructor(
    private adminService: AdminService,
    private toastSvc:     ToastService
  ) {}

  ngOnInit(): void {
    this.loadEmployees();
  }

  loadEmployees() {
    this.adminService.getEmployees()
      .subscribe((data: Employee[]) => this.employees = data);
  }

  // ---- search ----
  onSearchChanged() {
    this.page = 1;
    const q = this.searchTerm.trim();
    if (!q) {
      this.loadEmployees();
      return;
    }
    this.adminService.searchEmployees(q)
      .subscribe((data: Employee[]) => this.employees = data);
  }

  // ---- register ----
  submitRegisterForm(f: NgForm) {
    this.empSubmitted = true;
    if (f.invalid) return;
    this.registerEmployee();
  }

  registerEmployee() {
    this.adminService.registerEmployee(this.form)
      .subscribe(() => {
        this.toastSvc.show(`Employee registered: “${this.form.emailOrUsername}”`);
        this.form = { emailOrUsername: '', password: '' };
        this.empSubmitted = false;
        this.loadEmployees();
      });
  }

  // ---- delete ----
  promptDeleteEmployee(emp: Employee) {
    this.toDeleteEmployee = emp;
    this.confirmingDelete   = true;
  }

  onDeleteConfirmed() {
    if (!this.toDeleteEmployee) return;
    const id   = this.toDeleteEmployee.employeeID;
    const name = this.toDeleteEmployee.username || this.toDeleteEmployee.email;
    this.adminService.deleteEmployee(id)
      .subscribe(() => {
        this.toastSvc.show(`Employee deleted: “${name}”`);
        this.loadEmployees();
      });
    this.toDeleteEmployee   = null;
    this.confirmingDelete   = false;
  }

  onDeleteCanceled() {
    this.toDeleteEmployee = null;
    this.confirmingDelete = false;
  }

  // ---- edit ----
  setEdit(employee: Employee) {
    this.editing      = true;
    this.editForm     = { ...employee };
    this.editSubmitted = false;
  }

  submitEditForm(f: NgForm) {
    this.editSubmitted = true;
    if (f.invalid) return;
    this.updateEmployee();
  }

  updateEmployee() {
    this.adminService.updateEmployee(this.editForm.employeeID, this.editForm)
      .subscribe(() => {
        this.toastSvc.show(`Employee updated: “${this.editForm.username}”`);
        this.editing       = false;
        this.editSubmitted = false;
        this.loadEmployees();
      });
  }

  cancelEdit() {
    this.editing  = false;
    this.editForm = { employeeID: 0, username: '', email: '', role: '' };
  }
}
