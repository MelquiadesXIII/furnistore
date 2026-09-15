export type Product = {
  id: number;
  name: string;
  price: number;
  productCategoryId: number;
  imageUrl: string | null;
};

export type Paged<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
};
