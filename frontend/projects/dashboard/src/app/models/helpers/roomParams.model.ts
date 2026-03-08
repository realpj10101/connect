import { OrderByEnum } from "../../enums/order-by-enum";
import { PaginationParams } from "./paginationParams.model";

export class RoomParams extends PaginationParams
{
    orderBy: OrderByEnum = OrderByEnum.ALL;
    search: string = '';
}