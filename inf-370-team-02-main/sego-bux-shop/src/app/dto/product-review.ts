// src/app/dto/product-review.ts
export interface ProductReviewDto {
  reviewID: number;
  productID: number;
  userID: number;
  orderID: number;
  rating: number;
  reviewTitle?: string;
  reviewText: string;
  photoUrl?: string;
  submittedDate: string;
  status: string;
  userName: string;
  productName?: string; // if backend sends it we’ll use it
}

export interface CreateProductReviewDto {
  productID: number;
  orderID: number;
  rating: number;
  reviewTitle?: string;
  reviewText: string;
  photo?: File | null;
}
