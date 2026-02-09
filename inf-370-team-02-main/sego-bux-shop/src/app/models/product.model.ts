// product.model.ts
import { ProductImage } from './product-image.model';
import { ProductType } from './product-type.model';

export interface Product {
  productID:       number;
  name:            string;
  description:     string;
  price:           number;
  stockQuantity:   number;
  productTypeID:   number;
  primaryImageID?: number;
  secondaryImageID?: number; 
  productImages:   ProductImage[];
  lowStockThreshold: number;   
  productType?: ProductType;
}

