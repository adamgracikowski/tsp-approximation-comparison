import argparse
import glob
import os

def load_matrix_from_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    lines = [line.strip() for line in lines if line.strip() and not line.strip().startswith('#')]

    if not lines:
        raise ValueError("The file is empty.")

    dimension = int(lines[0])
    matrix = []

    for i in range(1, dimension + 1):
        row = list(map(int, lines[i].split()))
        if len(row) != dimension:
            raise ValueError(f"Row {i} has {len(row)} elements, expected {dimension}.")
        matrix.append(row)

    return dimension, matrix

def check_triangle_inequality(dimension, matrix):
    for i in range(dimension):
        for j in range(dimension):
            for k in range(dimension):
                if i != j and j != k and i != k:
                    if matrix[i][j] > matrix[i][k] + matrix[k][j]:
                        return False, (i, j, k)
    return True, None

def main():
    parser = argparse.ArgumentParser(description="Checks if .txt files satisfy the triangle inequality.")
    parser.add_argument("-d", "--dir", type=str, default=".", help="Directory to scan for .txt files (default: current directory).")
    
    args = parser.parse_args()

    if not os.path.exists(args.dir):
        print(f"Error: Directory '{args.dir}' does not exist.")
        return

    search_pattern = os.path.join(args.dir, "*.txt")
    tsp_files = glob.glob(search_pattern)

    if not tsp_files:
        print(f"No .txt files found in directory: {args.dir}")
        return

    print(f"Found {len(tsp_files)} .txt file(s). Starting verification...\n")

    passed_count = 0
    failed_count = 0

    for filepath in tsp_files:
        filename = os.path.basename(filepath)
        try:
            dimension, matrix = load_matrix_from_file(filepath)
            
            is_valid, violation = check_triangle_inequality(dimension, matrix)

            if is_valid:
                print(f"[OK] {filename} (Vertices: {dimension}) - Triangle inequality satisfied.")
                passed_count += 1
            else:
                i, j, k = violation
                d_ij = matrix[i][j]
                d_ik = matrix[i][k]
                d_kj = matrix[k][j]
                print(f"[FAILED] {filename} - Violation found!")
                print(f"         d({i}, {j}) = {d_ij} is strictly greater than d({i}, {k}) + d({k}, {j}) = {d_ik} + {d_kj} = {d_ik + d_kj}")
                failed_count += 1

        except Exception as e:
            print(f"[ERROR] {filename} - Could not process file. Reason: {e}")

    print("\n--- Summary ---")
    print(f"Total checked: {len(tsp_files)}")
    print(f"Passed: {passed_count}")
    print(f"Failed: {failed_count}")

if __name__ == "__main__":
    main()