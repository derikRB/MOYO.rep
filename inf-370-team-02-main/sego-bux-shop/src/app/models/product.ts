export interface Product {
  productID:     number;
  name:          string;
  description?:  string;
  price:         number;
  stockQuantity: number;
  productTypeID: number;
    lowStockThreshold: number;   // <-- ADD THIS

}
