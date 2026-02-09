export interface OrderLineDto {
  productID:         number;
  quantity:          number;
  template?:         string;
  customText?:       string;
  font?:             string;
  fontSize?:         number;
  color?:            string;
  uploadedImagePath?: string;
}

export interface OrderDto {
  customerID:      number;
  orderStatusID:   number;
  totalPrice:      number;
  deliveryMethod:  string;
  deliveryAddress: string;
  courierProvider: string;
  orderLines:      OrderLineDto[];
}
