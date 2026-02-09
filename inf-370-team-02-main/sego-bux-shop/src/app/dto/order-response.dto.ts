export interface OrderLineResponseDto {
  orderID: number;
  orderLineID: number;
  productID: number | null;        // ← can be null after soft delete
  productName: string;
  quantity: number;
  template?: string | null;
  customText?: string | null;
  font?: string | null;
  fontSize?: number | null;
  color?: string | null;
  uploadedImagePath?: string | null;
  snapshotPath?: string | null;
  productImageUrl?: string | null;
}

export interface OrderResponseDto {
  orderID: number;
  customerID: number;
  customerName: string;
  customerEmail: string;
  customerSurname?: string;
  customerPhone?: string;
  expectedDeliveryDate?: string | null;

  orderStatusID: number;
  orderStatusName: string;

  orderDate: string;
  totalPrice: number;

  deliveryMethod: string;
  deliveryAddress: string;
  courierProvider: string | null;   // ← backend may return null

  deliveryStatus: string;
  waybillNumber?: string | null;

  orderLines: OrderLineResponseDto[];
}
