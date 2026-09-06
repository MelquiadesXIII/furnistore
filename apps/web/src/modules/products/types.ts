export type Product = {
  id: number;
  name: string;
  price: number;
  productCategoryId: number;
};

export type Paged<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
};
