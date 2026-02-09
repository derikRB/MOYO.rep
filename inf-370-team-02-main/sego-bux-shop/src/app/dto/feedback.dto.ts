export interface FeedbackDto {
  feedbackID: number;
  userID: number;
  orderID: number;
  rating: number;
  comments: string;
  recommend: boolean;
  submittedDate: string;
  /** Optional (sent by backend if available) */
  userName?: string;
}

export interface CreateFeedbackDto {
  orderID: number;
  rating: number;     // 1–5
  comments: string;
  recommend: boolean;
}
