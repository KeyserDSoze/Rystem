import { Expose } from 'class-transformer';
export class IperUser {
    @Expose({ name: 'id' })
    identifier!: string;
    @Expose()
    name!: string;
    @Expose()
    groupId!: string;
    @Expose()
    port!: number;
}