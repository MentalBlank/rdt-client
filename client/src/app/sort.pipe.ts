import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'sort',
})
export class SortPipe implements PipeTransform {
  transform(array: any[], field: string, order: 'asc' | 'desc' = 'asc', caseSensitive?: boolean): any[] {
    if (!Array.isArray(array)) {
      return [];
    }

    const sortedArray = array.sort((a, b) => {
      let fieldA = a[field];
      let fieldB = b[field];
      
      if (typeof caseSensitive !== 'undefined' && caseSensitive === false) {
        fieldA = typeof fieldA?.toLowerCase !== 'undefined' && fieldA != null ? fieldA.toLowerCase() : fieldA;
        fieldB = typeof fieldB?.toLowerCase !== 'undefined' && fieldB != null ? fieldB.toLowerCase() : fieldB;
      }

      if (fieldA < fieldB) {
        return -1;
      } else if (fieldA > fieldB) {
        return 1;
      }

      return 0;
    });

    return order === 'asc' ? sortedArray : sortedArray.reverse();
  }
}