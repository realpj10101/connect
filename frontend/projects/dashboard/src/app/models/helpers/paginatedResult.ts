import { Pagination } from "./pagination";

export class PaginatedResult<T> {
    pagination?: Pagination; // api's response pagination values
    body?: T; // api's response body
}
