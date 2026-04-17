// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  username: string;
  email: string;
  roles: string[];
}

// ─── Issues ───────────────────────────────────────────────────────────────────

export type IssueStatus = "Open" | "InProgress" | "Resolved" | "Closed";
export type IssuePriority = "Low" | "Medium" | "High" | "Critical";

export interface IssueDto {
  id: string;
  title: string;
  description: string;
  status: IssueStatus;
  priority: IssuePriority;
  assigneeId?: string;
  assigneeName?: string;
  createdAt: string;
  updatedAt: string;
  tags: string[];
  commentsCount: number;
}

export interface CreateIssueRequest {
  title: string;
  description: string;
  priority: IssuePriority;
  assigneeId?: string;
  tags?: string[];
}

export interface UpdateIssueRequest {
  title?: string;
  description?: string;
  priority?: IssuePriority;
}

// ─── Inventory ────────────────────────────────────────────────────────────────

export type ProductCategory =
  | "Electronics"
  | "Food"
  | "Beverages"
  | "Clothing"
  | "Furniture"
  | "Books"
  | "Toys"
  | "Sports"
  | "Tools"
  | "Automotive"
  | "Health"
  | "Medical"
  | "Beauty"
  | "Home"
  | "Garden"
  | "Office"
  | "Pet"
  | "Baby"
  | "Other";

export type StockStatus = "InStock" | "LowStock" | "OutOfStock" | "Overstock" | "Discontinued";

export type StockMovementType =
  | "InitialStock"
  | "StockIn"
  | "StockOut"
  | "Adjustment"
  | "Transfer"
  | "Sale"
  | "Purchase"
  | "Return"
  | "Damage"
  | "Loss"
  | "Expired"
  | "LocationChanged"
  | "PriceAdjustment"
  | "Updated"
  | "Discontinued";

export interface ProductListDto {
  id: string;
  name: string;
  sku: string;
  category: string;
  currentStock: number;
  unitPrice: number;
  stockStatus: StockStatus;
  isActive: boolean;
  createdAt: string;
}

export interface ProductDto {
  id: string;
  name: string;
  sku: string;
  barcode?: string;
  description?: string;
  category: string;
  currentStock: number;
  minimumStockLevel: number;
  maximumStockLevel: number;
  unitPrice: number;
  costPrice: number;
  unit: string;
  currency: string;
  locationId?: string;
  supplierId?: string;
  expiryDate?: string;
  stockStatus: StockStatus;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateProductRequest {
  name: string;
  sku: string;
  category: ProductCategory;
  minimumStockLevel: number;
  maximumStockLevel: number;
  unitPrice: number;
  costPrice: number;
  description?: string;
  barcode?: string;
  unit?: string;
  currency?: string;
  locationId?: string;
  supplierId?: string;
  expiryDate?: string;
}

export interface UpdateProductRequest {
  name: string;
  description?: string;
  category: ProductCategory;
  minimumStockLevel: number;
  maximumStockLevel: number;
  barcode?: string;
  unit: string;
  currency: string;
  locationId?: string;
  supplierId?: string;
  expiryDate?: string;
}

export interface AdjustStockRequest {
  quantity: number;
  movementType: StockMovementType;
  reference?: string;
  notes?: string;
}

export interface StockMovementDto {
  id: string;
  productId: string;
  productName?: string;
  productSKU?: string;
  movementType: string;
  quantity: number;
  locationId?: string;
  locationName?: string;
  reference?: string;
  notes?: string;
  movementDate: string;
}

export interface SupplierListDto {
  id: string;
  name: string;
  code: string;
  contactPerson?: string;
  email?: string;
  isActive: boolean;
}

export interface LocationListDto {
  id: string;
  name: string;
  code: string;
  type: string;
  capacity: number;
  isActive: boolean;
}

/** @deprecated use ProductDto instead */
export interface InventoryItemDto {
  id: string;
  name: string;
  sku: string;
  quantity: number;
  location: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

// ─── Pagination ───────────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// ─── Analytics ────────────────────────────────────────────────────────────────

export interface AnalyticsSummaryDto {
  totalIssues: number;
  openIssues: number;
  resolvedIssues: number;
  closedIssues: number;
  totalInventoryItems: number;
  issuesByStatus: Record<IssueStatus, number>;
  issuesByPriority: Record<IssuePriority, number>;
  issuesByDay: { date: string; count: number }[];
}

// ─── Admin / Users ────────────────────────────────────────────────────────────

export interface UserAdminDto {
  id: string;
  username: string;
  email: string;
  fullName: string;
  roles: string[];
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

export interface RoleDto {
  id: string;
  name: string;
  description?: string;
}

export interface InviteUserRequest {
  username: string;
  email: string;
  fullName: string;
  role: string;
}

export interface UpdateUserRoleRequest {
  roleName: string;
}
